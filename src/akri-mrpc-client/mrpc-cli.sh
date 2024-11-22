#!/bin/env bash

if ! command -v uuidgen &> /dev/null
then
    sudo apt-get install uuid-runtime
else
    echo "uuidgen is installed"
fi

# Check if java is installed
if ! command -v java &> /dev/null
then
  echo "Java is not installed. Installing Microsoft distribution of Java 11..."
  wget https://aka.ms/download-jdk/microsoft-jdk-11.0.12.7.1-linux-x64.tar.gz
  sudo tar -xzf microsoft-jdk-11.0.12.7.1-linux-x64.tar.gz -C /opt/
  sudo update-alternatives --install /usr/bin/java java /opt/microsoft-jdk-11.0.12.7.1-linux-x64/bin/java 1
  rm microsoft-jdk-11.0.12.7.1-linux-x64.tar.gz
else
  echo "Java is already installed"
fi

if ! command -v mqtt &> /dev/null
then
  echo "mqtt cli is not installed. installing ..."
  wget https://github.com/hivemq/mqtt-cli/releases/download/v4.31.0/mqtt-cli-4.31.0.deb
  sudo dpkg -i mqtt-cli-4.31.0.deb
else
  echo "mqtt cli is installed"
fi

# Generate a short random lowercase string for CLIENT_ID
CLIENT_ID=$(tr -dc 'a-z' < /dev/urandom | head -c 8)

CORRELATION_ID=$(uuidgen)
RESPONSE_TOPIC="clients/$CLIENT_ID"

# Run mqtt sub in the background and capture output with a timeout
timeout 10s mqtt sub -t $RESPONSE_TOPIC -q 1 -up InvokerClientId=$CLIENT_ID | tee sub_output.log &

# Give the subscriber a moment to start
sleep 2

# Publish the message to the specified topic
mqtt pub -t "akri/discovery/dtmi:com:microsoft:deviceregistry:discoveredassetresources;1/$CLIENT_ID/command/createDiscoveredAsset" \
-m "{\"createDiscoveredAssetRequest\": { \"assetEndpointProfileRef\": \"mrpcdaep-abc123\", \"assetName\": \"mrpctest-$CLIENT_ID\", \"manufacturer\": \"TestManufacturer\", \"manufacturerUri\": \"http://test.com\", \"model\": \"TestModel\", \"productCode\": \"TestCode\", \"hardwareRevision\": \"v1\", \"softwareRevision\": \"v1.0\", \"documentationUri\": \"http://docs.test.com\", \"serialNumber\": \"SN123456\", \"defaultTopic\": { \"path\": \"akri/topic\", \"retain\": true }, \"dataSets\": [ { \"dataPoints\": [ { \"dataPointConfiguration\": \"{\\\"publishingInterval\\\":8,\\\"samplingInterval\\\":8,\\\"queueSize\\\":4}\", \"dataSource\": \"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt1\", \"lastUpdatedOn\": null, \"name\": \"datapoint1\" } ], \"dataSetConfiguration\": \"{\\\"publishingInterval\\\":10,\\\"samplingInterval\\\":15,\\\"queueSize\\\":20}\", \"name\": null, \"topic\": { \"path\": \"dataset/topic/1\", \"retain\": true } } ], \"defaultDatasetsConfiguration\": \"{\\\"publishingInterval\\\":10,\\\"samplingInterval\\\":15,\\\"queueSize\\\":20}\", \"defaultEventsConfiguration\": null, \"events\": [ { \"eventConfiguration\": \"{\\\"publishingInterval\\\":10,\\\"samplingInterval\\\":15,\\\"queueSize\\\":20}\", \"eventNotifier\": \"nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt1\", \"lastUpdatedOn\": null, \"name\": \"event1\", \"topic\": { \"path\": \"event/topic/1\", \"retain\": true } } ] }}" \
-q 1 \
-ct "application/json" \
-cd $CORRELATION_ID \
-pf UTF_8 \
-i $CLIENT_ID \
-rt $RESPONSE_TOPIC \
-up InvokerClientId=$CLIENT_ID

# Wait for background processes to complete
wait

# Extract the status from the response using jq
STATUS=$(jq -r '.createDiscoveredAssetResponse.status' sub_output.log)

# Check the status and exit with the appropriate code
if [ "$STATUS" == "success" ]; then
    exit 0
else
    exit 1
fi