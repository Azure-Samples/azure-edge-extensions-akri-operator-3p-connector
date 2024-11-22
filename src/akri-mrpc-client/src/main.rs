
use akri_mqtt::connection_settings::ConnectionSettings;
use akri_mrpc_v1::dtmi_com_microsoft_deviceregistry_DiscoveredAssetResources__1::wrapper::DiscoveredAssetResources;
use akri_mrpc_client::RpcCommandRunner;
use ctrlc;
use futures::lock::Mutex;
use paho_mqtt::{self as mqtt, MQTT_VERSION_5};
use std::fs;
use std::path::Path;
use std::process;
use std::sync::Arc;
use std::thread::{self, Scope};
use std::time::Duration;
use tokio::{runtime, task};
use uuid::Uuid;

const MQTT_SAT_AUTH_METHOD: &str = "K8S-SAT";
const HELM_SAT_AUTH_METHOD: &str = "serviceAccountToken";
const CA_PATH: &str = "/var/run/secrets/ca";
const CA_COMBINED_FILE: &str = "/var/run/secrets/combined-cert.crt";

fn main() {
    thread::scope(|scope| {
        let runtime = runtime::Builder::new_multi_thread()
            .enable_all()
            .build()
            .unwrap();
        runtime.block_on(async_main(scope))
    })
}

async fn async_main<'scope, 'env>(scope: &'scope Scope<'scope, 'env>) {
    let conn_settings = ConnectionSettings::from_env_vars();

    let mqtt_auth_method = std::env::var("MQTT_AUTH_METHOD").unwrap_or_else(|_| String::new());
    let sat_path_string = std::env::var("MQTT_SAT_PATH").unwrap_or_else(|_| String::new());

    let mqtt_scheme = if conn_settings.mqtt_use_tls {
        "mqtts"
    }
    else {
        "mqtt"
    };

    let mqtt_host = format!("{mqtt_scheme}://{}:{}", conn_settings.mqtt_hostname, conn_settings.mqtt_tcp_port);

    let mqtt_client = Arc::new(Mutex::new(
        mqtt::AsyncClient::new((mqtt_host, Uuid::new_v4().to_string())).unwrap(),
    ));


    let mut conn_options_builder = mqtt::ConnectOptionsBuilder::with_mqtt_version(MQTT_VERSION_5);
    conn_options_builder.clean_start(conn_settings.mqtt_clean_start);
    conn_options_builder.automatic_reconnect(Duration::from_secs(2), Duration::from_secs(120));

    if mqtt_auth_method == HELM_SAT_AUTH_METHOD {
        println!("Using Service Account Token for Authentication.");

        let sat_token_bytes = fs::read(Path::new(&sat_path_string.as_str())).unwrap();
   
        conn_options_builder.properties(mqtt::properties![
            mqtt::PropertyCode::AuthenticationMethod => MQTT_SAT_AUTH_METHOD,
            mqtt::PropertyCode::AuthenticationData => sat_token_bytes
            ]);
    } else {
        println!("Not using MQTT Authentication.");
    }

    if conn_settings.mqtt_use_tls {
        println!("Using TLS to connect.");

        let mut ssl_options_builder = mqtt::SslOptionsBuilder::new();
        if let Some(ca_file) = conn_settings.mqtt_ca_file {
            ssl_options_builder.trust_store(ca_file).unwrap();
        } else {
            akri_shared::utilities::cert::read_root_ca_certs_to_file(CA_PATH, CA_COMBINED_FILE).expect("Failed to read root CA certs to file");
            ssl_options_builder.trust_store(CA_COMBINED_FILE).unwrap();
        }
        if let (Some(client_cert), Some(client_key)) = (conn_settings.mqtt_cert_file, conn_settings.mqtt_key_file) {
            ssl_options_builder.key_store(client_cert).unwrap();
            ssl_options_builder.private_key(client_key).unwrap();
        }
        conn_options_builder.ssl_options(ssl_options_builder.finalize());
    } else {
        println!("Not using TLS to connect.");
    }

    let conn_opts = conn_options_builder.finalize();

    mqtt_client
        .lock()
        .await
        .connect(conn_opts)
        .await
        .expect("Failed to connect to MQTT broker");

    println!("Connected to the MQTT broker");

    let client = Arc::new(Mutex::new(DiscoveredAssetResources::Client::new(mqtt_client.clone())));

    client.lock().await.start(scope);

    let rpc_command_invoker = Arc::new(Mutex::new(RpcCommandRunner::new(&client)));

    ctrlc::set_handler( || {
        process::exit(0);
    }).unwrap();

    let create_discovered_asset_task = task::spawn(RpcCommandRunner::run_akri_commands(rpc_command_invoker.clone()));
    create_discovered_asset_task.await.unwrap();
    println!("Disconnecting from the MQTT broker");
    let _ = mqtt_client.lock().await.disconnect(None).await;
}
