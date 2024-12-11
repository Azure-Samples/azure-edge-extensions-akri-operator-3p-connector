K3DCLUSTERNAME := devcluster
K3DREGISTRYNAME := k3d-devregistry.localhost:5500
PORTFORWARDING := -p '8883:8883@loadbalancer' -p '1883:1883@loadbalancer'
VERSION := $(shell grep "<ContainerImageTag>" ./src/akri-connector-sample/TcpConnector/TcpConnector.csproj | sed 's/[^0-9.]*//g')

all: infra deploy_assets deploy_asset_endpoint_profile deploy_3p_connector

sample: infra deploy_assets_sample deploy_asset_endpoint_profile_sample deploy_3p_connector_sample

infra: create_k3d_cluster deploy_aio_mqtt_broker deploy_rest_server install_aep_asset_crds install_akri_operator deploy_mqttui

create_k3d_cluster:
	@echo "Creating k3d cluster..."
	k3d cluster create $(K3DCLUSTERNAME) $(PORTFORWARDING) --servers 1

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
	helm install akri-operator oci://akripreview.azurecr.io/helm/microsoft-managed-akri-operator --version 0.1.5-preview -n azure-iot-operations

deploy_asset_endpoint_profile:
	@echo "Deploying AssetEndpointProfile"
	kubectl apply -f ./deploy/tcp-asset-endpoint-profile-definition.yaml

deploy_asset_endpoint_profile_sample:
	@echo "Deploying AssetEndpointProfile"
	kubectl apply -f ./deploy/rest-server-asset-endpoint-profile-definition.yaml

deploy_assets:
	@echo "Deploying Assets"
	kubectl apply -f ./deploy/tcp-asset-definition.yaml

deploy_assets_sample:
	@echo "Deploying Assets"
	kubectl apply -f ./deploy/rest-server-asset-definition.yaml

deploy_3p_connector: build_3p_connector_image deploy_3p_connector_config

deploy_3p_connector_sample: deploy_3p_connector_config_sample

build_3p_connector_image:
	@echo "Building 3p Connector Image"
	docker build . -f ./src/akri-connector-sample/TcpConnector/Dockerfile -t tcpconnector:$(VERSION)
	k3d image import tcpconnector:$(VERSION) -c $(K3DCLUSTERNAME)

deploy_3p_connector_config:
	@echo "Deploying 3p Connector Config"
	# on a mac (sed -i '' "s?__{image_version}__?$(VERSION)?g" ./deploy/connector-config.yaml)
	sed -i '' "s?__{image_version}__?$(VERSION)?g" ./deploy/connector-config.yaml
	kubectl apply -f ./deploy/connector-config.yaml

deploy_3p_connector_config_sample:
	@echo "Deploying 3p Connector Config Sample"
	kubectl apply -f ./deploy/connector-config-sample.yaml

deploy_mqttui:
	@echo "Deploying MQTT UI"
	kubectl apply -f ./deploy/mqttclient.yaml

clean:
	@echo "Cleaning up..."
	k3d cluster delete $(K3DCLUSTERNAME)