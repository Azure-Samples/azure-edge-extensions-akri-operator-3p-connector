# Akri Mrpc Client

This mrpc client is used for testing the extension service mrpc server. It sends commands to create discovered assets and 
discovered asset endpoint profiles. 


## Mqtt Cli commands

```sh 
mosquitto_pub -t "your/topic" -m "your message" \
-D "" "__ts" "your_timestamp_value" \
-D "user" "__ft" "your_fencing_token_value" \
-D "user" "__invId" "your_invoker_client_id_value" \
-D "system" "ContentType" "application/json" \
-D "system" "FormatIndicator" "1" \
-D "system" "CorrelationData" "your_correlation_data_value" \
-D "system" "ResponseTopic" "your_response_topic_value" \
-D "system" "MessageExpiry" "your_message_expiry_value"


wget https://github.com/hivemq/mqtt-cli/releases/download/v4.30.0/mqtt-cli-4.30.0.deb
sudo apt install ./mqtt-cli-4.30.0.deb

mqtt pub -t "akri/discovery/dtmi:com:microsoft:deviceregistry:DiscoveredAssetResources;1/TestCli/command/createDiscoveredAssetRequest" \
-m "{ "asset_endpoint_profile_ref": "mrpcdaep-abc123", "asset_name": "mrpctestabc123", "manufacturer": "TestManufacturer", "manufacturer_uri": "http://test.com", "model": "TestModel", "product_code": "TestCode", "hardware_revision": "v1", "software_revision": "v1.0", "documentation_uri": "http://docs.test.com", "serial_number": "SN123456", "default_topic": { "path": "akri/topic", "retain": true }, "data_sets": [ { "data_points": [ { "data_point_configuration": "{\"publishingInterval\":8,\"samplingInterval\":8,\"queueSize\":4}", "data_source": "nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt1", "last_updated_on": null, "name": "datapoint1" } ], "data_set_configuration": "{\"publishingInterval\":10,\"samplingInterval\":15,\"queueSize\":20}", "name": null, "topic": { "path": "dataset/topic/1", "retain": true } } ], "default_datasets_configuration": "{\"publishingInterval\":10,\"samplingInterval\":15,\"queueSize\":20}", "default_events_configuration": null, "events": [ { "event_configuration": "{\"publishingInterval\":10,\"samplingInterval\":15,\"queueSize\":20}", "event_notifier": "nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt1", "last_updated_on": null, "name": "event1", "topic": { "path": "event/topic/1", "retain": true } } ] }" \
-q 1 \
-ct "application/json" \
-cd ""
-pf 1 \
-rt akri/discovery/cli \
-up InvokerClientId=TestCli


```