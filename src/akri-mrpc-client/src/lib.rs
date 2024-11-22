use futures::lock::Mutex;
use paho_mqtt::{self as mqtt};
use std::sync::Arc;
use std::process;
use serde::{Deserialize, Serialize};
use schemars::JsonSchema;
use std::iter;
use akri_mrpc_v1::dtmi_com_microsoft_deviceregistry_DiscoveredAssetResources__1::object_create_discovered_asset_request::Object_CreateDiscoveredAsset_Request;
use akri_mrpc_v1::dtmi_com_microsoft_deviceregistry_DiscoveredAssetResources__1::create_discovered_asset_command_request::CreateDiscoveredAssetCommandRequest;
use akri_mrpc_v1::dtmi_com_microsoft_deviceregistry_DiscoveredAssetResources__1::wrapper::DiscoveredAssetResources;

use rand::Rng;

use tokio::task;
use akri_mrpc_v1::dtmi_com_microsoft_deviceregistry_DiscoveredAssetResources__1::create_discovered_asset_endpoint_profile_command_request::CreateDiscoveredAssetEndpointProfileCommandRequest;
use akri_mrpc_v1::dtmi_com_microsoft_deviceregistry_DiscoveredAssetResources__1::enum_com_microsoft_deviceregistry_discovered_topic_retain__1::Enum_Com_Microsoft_Deviceregistry_DiscoveredTopicRetain__1;
use akri_mrpc_v1::dtmi_com_microsoft_deviceregistry_DiscoveredAssetResources__1::enum_create_discovered_asset_endpoint_profile_request_supported_authentication_methods_element_schema::Enum_CreateDiscoveredAssetEndpointProfile_Request_SupportedAuthenticationMethods_ElementSchema;
use akri_mrpc_v1::dtmi_com_microsoft_deviceregistry_DiscoveredAssetResources__1::object_create_discovered_asset_endpoint_profile_request::Object_CreateDiscoveredAssetEndpointProfile_Request;
use akri_mrpc_v1::dtmi_com_microsoft_deviceregistry_DiscoveredAssetResources__1::object_create_discovered_asset_request_datasets_element_schema::Object_CreateDiscoveredAsset_Request_Datasets_ElementSchema;
use akri_mrpc_v1::dtmi_com_microsoft_deviceregistry_DiscoveredAssetResources__1::object_create_discovered_asset_request_datasets_element_schema_data_points_element_schema::Object_CreateDiscoveredAsset_Request_Datasets_ElementSchema_DataPoints_ElementSchema;
use akri_mrpc_v1::dtmi_com_microsoft_deviceregistry_DiscoveredAssetResources__1::object_create_discovered_asset_request_datasets_element_schema_topic::Object_CreateDiscoveredAsset_Request_Datasets_ElementSchema_Topic;
use akri_mrpc_v1::dtmi_com_microsoft_deviceregistry_DiscoveredAssetResources__1::object_create_discovered_asset_request_default_topic::Object_CreateDiscoveredAsset_Request_DefaultTopic;
use akri_mrpc_v1::dtmi_com_microsoft_deviceregistry_DiscoveredAssetResources__1::object_create_discovered_asset_request_events_element_schema::Object_CreateDiscoveredAsset_Request_Events_ElementSchema;
use akri_mrpc_v1::dtmi_com_microsoft_deviceregistry_DiscoveredAssetResources__1::object_create_discovered_asset_request_events_element_schema_topic::Object_CreateDiscoveredAsset_Request_Events_ElementSchema_Topic;

#[derive(Debug, Clone, PartialEq, Serialize, Deserialize, JsonSchema)]
pub enum V1AuthenticationMode {
    #[serde(alias = "anonymous")]
    Anonymous = 0,
    #[serde(alias = "usernamePassword")]
    UsernamePassword = 1,
    #[serde(alias = "certificate")]
    Certificate = 2,
}

#[derive(Serialize, Deserialize, Debug, Clone, JsonSchema, PartialEq)]
pub struct V1UserAuthenticationConfiguration {
    /// The authentication mode.
    #[serde(alias = "Mode")]
    pub mode: V1AuthenticationMode,

    /// The user name file path.
    #[serde(rename = "UserNameFile", alias = "userNameFile")]
    pub user_name_file: Option<String>,

    /// The password file path.
    #[serde(rename = "PasswordFile", alias = "passwordFile")]
    pub password_file: Option<String>,

    /// The certificate file path.
    #[serde(rename = "CertificateFile", alias = "certificateFile")]
    pub certificate_file: Option<String>,
}

#[derive(Debug)]
pub struct GenericEndpointProfile {
    pub name: String,
    pub target_address: String,
    pub user_authentication: V1UserAuthenticationConfiguration,
    pub additional_configuration: String,
}

fn generate(len: usize) -> String {
    const CHARSET: &[u8] = b"abcdefghijklmnopqrstuvwxyz0123456789";
    let mut rng = rand::thread_rng();
    let one_char = || CHARSET[rng.gen_range(0..CHARSET.len())] as char;
    iter::repeat_with(one_char).take(len).collect()
}


pub struct RpcCommandRunner {
    client: Arc<Mutex<DiscoveredAssetResources::Client<mqtt::async_client::AsyncClient>>>,
}

impl RpcCommandRunner {
    pub fn new(
        client: &Arc<Mutex<DiscoveredAssetResources::Client<mqtt::async_client::AsyncClient>>>
    ) -> Self {
        Self {
            client: client.clone()
        }
    }

    pub async fn run_akri_commands(this: Arc<Mutex<RpcCommandRunner>>) {
        let me = this.lock().await;

        let random_name = task::block_in_place(|| {
            generate(6)
        });

        let mut daep_tasks = Vec::new();

        let mut tasks = Vec::new();

        let opc_daep = Object_CreateDiscoveredAssetEndpointProfile_Request {
            target_address: Some(format!("opc://{}:8080", random_name)),
            additional_configuration: Some("".to_string()),
            daep_name: Some(format!("opc-{}", random_name)),
            endpoint_profile_type: None,
            supported_authentication_methods: Some(vec![
                Enum_CreateDiscoveredAssetEndpointProfile_Request_SupportedAuthenticationMethods_ElementSchema::UsernamePassword
            ]),
        };
        let onvif_daep = Object_CreateDiscoveredAssetEndpointProfile_Request {
            target_address: Some(format!("http://{}:8080", random_name)),
            additional_configuration: Some("".to_string()),
            daep_name: Some(format!("onvif-{}", random_name)),
            endpoint_profile_type: None,
            supported_authentication_methods: Some(vec![
                Enum_CreateDiscoveredAssetEndpointProfile_Request_SupportedAuthenticationMethods_ElementSchema::UsernamePassword
            ]),
        };

        let media_broker_daep = Object_CreateDiscoveredAssetEndpointProfile_Request {
            target_address: Some(format!("rtsp://{}:554", random_name)),
            additional_configuration: Some("".to_string()),
            daep_name: None,
            endpoint_profile_type: None,
            supported_authentication_methods: Some(vec![
                Enum_CreateDiscoveredAssetEndpointProfile_Request_SupportedAuthenticationMethods_ElementSchema::UsernamePassword,
                Enum_CreateDiscoveredAssetEndpointProfile_Request_SupportedAuthenticationMethods_ElementSchema::Certificate
            ]),
        };

        daep_tasks.push(
            me.client
                .lock()
                .await
                .create_discovered_asset_endpoint_profile(&CreateDiscoveredAssetEndpointProfileCommandRequest {
                    create_discovered_asset_endpoint_profile_request: opc_daep,
                }).await
                .unwrap(),
        );

        daep_tasks.push(
            me.client
                .lock()
                .await
                .create_discovered_asset_endpoint_profile(&CreateDiscoveredAssetEndpointProfileCommandRequest {
                    create_discovered_asset_endpoint_profile_request: onvif_daep,
                }).await
                .unwrap(),
        );

        daep_tasks.push(
            me.client
                .lock()
                .await
                .create_discovered_asset_endpoint_profile(&CreateDiscoveredAssetEndpointProfileCommandRequest {
                    create_discovered_asset_endpoint_profile_request: media_broker_daep,
                }).await
                .unwrap(),
        );

        for _ in 0..3 {
            println!("Invoking CreateDiscoveredAssetCommandRequest...");
            let new_endpoint_profile = GenericEndpointProfile {
                name: format!("mrpcdaep-{}", random_name),
                target_address: format!("http://{}:8080", random_name),
                user_authentication: V1UserAuthenticationConfiguration {
                    mode: V1AuthenticationMode::Anonymous,
                    user_name_file: None,
                    password_file: None,
                    certificate_file: None,
                },
                additional_configuration: "".to_string(),
            };

            let random_asset_name = task::block_in_place(|| {
                generate(6)
            });


            let new_dasset = Object_CreateDiscoveredAsset_Request {
                asset_endpoint_profile_ref: Some(new_endpoint_profile.name.clone()),
                asset_name: Some(format!("mrpctest{}", random_asset_name)),
                manufacturer: Some("TestManufacturer".to_string()),
                manufacturer_uri: Some("http://test.com".to_string()),
                model: Some("TestModel".to_string()),
                product_code: Some("TestCode".to_string()),
                hardware_revision: Some("v1".to_string()),
                software_revision: Some("v1.0".to_string()),
                documentation_uri: Some("http://docs.test.com".to_string()),
                serial_number: Some("SN123456".to_string()),
                default_topic: Some(Object_CreateDiscoveredAsset_Request_DefaultTopic {
                    path: Some("akri/topic".to_string()),
                    retain: None,
                }),
                datasets: Some(vec![
                    Object_CreateDiscoveredAsset_Request_Datasets_ElementSchema {
                        data_points: Some(vec![
                            Object_CreateDiscoveredAsset_Request_Datasets_ElementSchema_DataPoints_ElementSchema {
                                data_point_configuration: Some("{\"publishingInterval\":8,\"samplingInterval\":8,\"queueSize\":4}".to_string()),
                                data_source: Some("nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt1".to_string()),
                                last_updated_on: None,
                                name: Some("datapoint1".to_string()),
                            }
                        ]),
                        data_set_configuration: Some("{\"publishingInterval\":10,\"samplingInterval\":15,\"queueSize\":20}".to_string()),
                        name: None,
                        topic: Some(Object_CreateDiscoveredAsset_Request_Datasets_ElementSchema_Topic {
                            path: Some("dataset/topic/1".to_string()),
                            retain: Some(Enum_Com_Microsoft_Deviceregistry_DiscoveredTopicRetain__1::Keep),
                        }),
                    }
                ]),
                default_datasets_configuration: Some("{\"publishingInterval\":10,\"samplingInterval\":15,\"queueSize\":20}".to_string()),
                default_events_configuration: None,
                events: Some(vec![
                    Object_CreateDiscoveredAsset_Request_Events_ElementSchema {
                        event_configuration: Some("{\"publishingInterval\":10,\"samplingInterval\":15,\"queueSize\":20}".to_string()),
                        event_notifier: Some("nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt1".to_string()),
                        last_updated_on: None,
                        name: Some("event1".to_string()),
                        topic: Some(Object_CreateDiscoveredAsset_Request_Events_ElementSchema_Topic {
                            path: Some("event/topic/1".to_string()),
                            retain: Some(Enum_Com_Microsoft_Deviceregistry_DiscoveredTopicRetain__1::Never),
                        })
                    }
                ])
            };
            tasks.push(
                me.client
                    .lock()
                    .await
                    .create_discovered_asset(&CreateDiscoveredAssetCommandRequest {
                        create_discovered_asset_request: new_dasset,
                    }).await
                    .unwrap(),
            );
        }

        let mut dup_tasks = Vec::new();

        let new_dasset_dup1 = Object_CreateDiscoveredAsset_Request {
            asset_endpoint_profile_ref: Some("store-camera".to_string()),
            asset_name: Some("store-camera-ptz".to_string()),
            manufacturer: Some("Happytimesoft".to_string()),
            manufacturer_uri: None,
            model: Some("IPCamera".to_string()),
            product_code: None,
            serial_number: Some("123456".to_string()),
            software_revision: Some("2.4".to_string()),
            datasets: None,
            default_datasets_configuration: None,
            default_events_configuration: None,
            default_topic: None,
            documentation_uri: None,
            events: None,
            hardware_revision: None,

        };



        let new_dasset_dup2 = Object_CreateDiscoveredAsset_Request {
            asset_endpoint_profile_ref: Some("store-camera".to_string()),
            asset_name: Some("store-camera-device".to_string()),
            manufacturer: Some("Happytimesoft".to_string()),
            manufacturer_uri: None,
            model: Some("IPCamera".to_string()),
            product_code: None,
            serial_number: Some("123456".to_string()),
            software_revision: Some("2.4".to_string()),
            datasets: None,
            default_datasets_configuration: None,
            default_events_configuration: None,
            default_topic: None,
            documentation_uri: None,
            events: None,
            hardware_revision: None,

        };

        dup_tasks.push(
            me.client
                .lock()
                .await
                .create_discovered_asset(&CreateDiscoveredAssetCommandRequest {
                    create_discovered_asset_request: new_dasset_dup1,
                }).await
                .unwrap(),
        );


        dup_tasks.push(
            me.client
                .lock()
                .await
                .create_discovered_asset(&CreateDiscoveredAssetCommandRequest {
                    create_discovered_asset_request: new_dasset_dup2,
                }).await
                .unwrap(),
        );



        let mut detect_dup_tasks = Vec::new();

        let detect_dasset_dup1 = Object_CreateDiscoveredAsset_Request {
            asset_endpoint_profile_ref: Some("store-camera".to_string()),
            asset_name: Some("store-camera".to_string()),
            manufacturer: Some("Happytimesoft".to_string()),
            manufacturer_uri: None,
            model: Some("IPCamera".to_string()),
            product_code: None,
            serial_number: Some("123456".to_string()),
            software_revision: Some("2.4".to_string()),
            datasets: None,
            default_datasets_configuration: None,
            default_events_configuration: None,
            default_topic: None,
            documentation_uri: None,
            events: None,
            hardware_revision: None,

        };



        let detect_dasset_dup2 = Object_CreateDiscoveredAsset_Request {
            asset_endpoint_profile_ref: Some("store-camera".to_string()),
            asset_name: Some("store-camera".to_string()),
            manufacturer: Some("Happytimesoft".to_string()),
            manufacturer_uri: None,
            model: Some("IPCamera".to_string()),
            product_code: None,
            serial_number: Some("123456".to_string()),
            software_revision: Some("2.4".to_string()),
            datasets: None,
            default_datasets_configuration: None,
            default_events_configuration: None,
            default_topic: None,
            documentation_uri: None,
            events: None,
            hardware_revision: None,

        };

        detect_dup_tasks.push(
            me.client
                .lock()
                .await
                .create_discovered_asset(&CreateDiscoveredAssetCommandRequest {
                    create_discovered_asset_request: detect_dasset_dup1,
                }).await
                .unwrap(),
        );


        detect_dup_tasks.push(
            me.client
                .lock()
                .await
                .create_discovered_asset(&CreateDiscoveredAssetCommandRequest {
                    create_discovered_asset_request: detect_dasset_dup2,
                }).await
                .unwrap(),
        );





        for task in daep_tasks {
            match task.await {
                Ok(response) => match response {
                    Ok(result) => {

                        println!("DAEP Create Result: {:?}", result.create_discovered_asset_endpoint_profile_response.status.unwrap());
                    }
                    Err(err) => {
                        println!(
                            "Create Discovered Asset Endpoint Profile failed {:?} {}",
                            err.kind(),
                            err.to_string()
                        );
                        process::exit(-1);
                    }
                },
                Err(err) => {
                    println!("Error: {}", err);
                    process::exit(1);
                }
            }
        }

        for task in tasks {
            match task.await {
                Ok(response) => match response {
                    Ok(result) => {
                        println!("DASSET Result: {:?}", result.create_discovered_asset_response.status.unwrap());
                    }
                    Err(err) => {
                        println!(
                            "Create Discovered Asset failed {:?} {}",
                            err.kind(),
                            err.to_string()
                        );
                        process::exit(-1);
                    }
                },
                Err(err) => {
                    println!("Error: {}", err);
                    process::exit(1);
                }
            }
        }

        for task in dup_tasks {
            match task.await {
                Ok(response) => match response {
                    Ok(result) => {
                        println!("DASSET dup Result: {:?}", result.create_discovered_asset_response.status.unwrap());
                    }
                    Err(err) => {
                        println!(
                            "Create Discovered Asset failed {:?} {}",
                            err.kind(),
                            err.to_string()
                        );
                        process::exit(-1);
                    }
                },
                Err(err) => {
                    println!("Error: {}", err);
                    process::exit(1);
                }
            }
        }


        for task in detect_dup_tasks {
            match task.await {
                Ok(response) => match response {
                    Ok(result) => {
                        println!("DASSET detect dup Result: {:?}", result.create_discovered_asset_response.status.unwrap());
                    }
                    Err(err) => {
                        println!(
                            "Create Discovered Asset failed {:?} {}",
                            err.kind(),
                            err.to_string()
                        );
                        process::exit(-1);
                    }
                },
                Err(err) => {
                    println!("Error: {}", err);
                    process::exit(1);
                }
            }
        }

    }
}
