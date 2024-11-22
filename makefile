K3DCLUSTERNAME := devcluster
K3DREGISTRYNAME := k3d-devregistry.localhost:5500
PORTFORWARDING := -p '8883:8883@loadbalancer' -p '1883:1883@loadbalancer'
VERSION := $(shell grep "<ContainerImageTag>" ./src/akri-connector-sample/RestThermostatConnectorApp/RestThermostatConnectorApp.csproj | sed 's/[^0-9.]*//g')

create_k3d_cluster:
	@echo "Creating k3d cluster..."
	k3d cluster create $(K3DCLUSTERNAME) $(PORTFORWARDING) --registry-use $(K3DREGISTRYNAME) --servers 1

deploy_aio_mqtt_broker:
	@echo "Deploying AIO MQTT Broker"
	bash ./deploy/aio-broker/iot-mq.sh

deploy_rest_server:
	@echo "Deploying REST Server"
	helm install rest-server oci://akribuilds.azurecr.io/helm/rest-server --version 0.1.7 -n azure-iot-operations

install_aep_asset_crds:
	@echo "Installing AssetEndpointProfile and Asset CRDs"
	helm install adr --version 1.0.0 oci://mcr.microsoft.com/azureiotoperations/helm/adr/assets-arc-extension -n azure-iot-operations

install_akri_operator:
	@echo "Installing AKRI Operator"
	helm install akri-operator oci://akripreview.azurecr.io/helm/microsoft-managed-akri-operator --version 0.1.2-preview -n azure-iot-operations

deploy_asset_endpoint_profile:
	@echo "Deploying AssetEndpointProfile"
	kubectl apply -f ./deploy/rest-server-aep1.yaml

deploy_assets:
	@echo "Deploying AssetEndpointProfile"
	kubectl apply -f ./deploy/rest-server-asset-endpoint-profile-definition.yaml

build_3p_connector:
	@echo "Building 3p Connector"
	docker build ./src -f ./src/akri-connector-sample/RestThermostatConnectorApp/Dockerfile -t restthermostatconnectorapp:$(VERSION)
	# docker save -o restthermostatconnectorapp.tar restthermostatconnectorapp:$(VERSION)
	# k3d image import restthermostatconnectorapp.tar -c $(K3DCLUSTERNAME)
	docker tag restthermostatconnectorapp:$(VERSION) $(K3DREGISTRYNAME)/restthermostatconnectorapp:$(VERSION)
	docker push $(K3DREGISTRYNAME)/restthermostatconnectorapp:$(VERSION)

deploy_3p_connector_config:
	@echo "Deploying 3p Connector"
	sed -i "s?__{container_registry}__?$(K3DREGISTRYNAME)?g" ./deploy/connector-config.yaml
	sed -i "s?__{image_version}__?$(VERSION)?g" ./deploy/connector-config.yaml
	kubectl apply -f ./deploy/connector-config.yaml

deploy_mqttui:
	@echo "Deploying MQTT UI"
	kubectl apply -f ./deploy/mqttclient.yaml