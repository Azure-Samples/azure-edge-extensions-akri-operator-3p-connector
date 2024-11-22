K3DCLUSTERNAME := devcluster
K3DREGISTRYNAME := k3d-devregistry.localhost:5500
PORTFORWARDING := -p '8883:8883@loadbalancer' -p '1883:1883@loadbalancer'
VERSION := $(shell grep "<ContainerImageTag>" ./src/AzureIoTOperations.DaprWorkflow/AzureIoTOperations.DaprWorkflow.csproj | sed 's/[^0-9.]*//g')

create_k3d_cluster:
	@echo "Creating k3d cluster..."
	k3d cluster create $(K3DCLUSTERNAME) $(PORTFORWARDING) --registry-use $(K3DREGISTRYNAME) --servers 1

deploy_aio_mqtt_broker:
	@echo "Deploying AIO MQTT Broker"
	bash ./deploy/aio-broker/iot-mq.sh

deploy_simple_http_server:
	@echo "Deploying HTTP Server"
	helm install http-server oci://akribuilds.azurecr.io/helm/http-server --version 0.1.2 -n azure-iot-operations

install_aep_asset_crds:
	@echo "Installing AssetEndpointProfile and Asset CRDs"
	helm install adr --version 1.0.0 oci://mcr.microsoft.com/azureiotoperations/helm/adr/assets-arc-extension -n azure-iot-operations

install_akri_operator:
	@echo "Installing AKRI Operator"
	helm install akri-operator oci://akripreview.azurecr.io/helm/microsoft-managed-akri-operator --version 0.1.1-preview -n azure-iot-operations

deploy_asset_endpoint_profile:
	@echo "Deploying AssetEndpointProfile"
	kubectl apply -f ./deploy/http-server-asset-endpoint-profile-definition.yaml

deploy_assets:
	@echo "Deploying AssetEndpointProfile"
	kubectl apply -f ./deploy/http-server-asset-definition-1.yaml
	kubectl apply -f ./deploy/http-server-asset-definition-2.yaml

build_3p_connector:
	@echo "Building 3p Connector"
	docker build ./src -f .src/akri-connector-sample/HttpThermostatConnectorApp/Dockerfile
	docker tag daprworkflow:$(VERSION) $(K3DREGISTRYNAME)/httpthermostatconnectorapp:$(VERSION)
	docker push $(K3DREGISTRYNAME)/httpthermostatconnectorapp:$(VERSION)

deploy_mqttui:
	@echo "Deploying MQTT UI"
	kubectl apply -f ./deploymqttclient.yaml