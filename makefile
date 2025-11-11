K3DCLUSTERNAME := devcluster
K3DREGISTRYNAME := k3d-devregistry.localhost:5500
PORTFORWARDING := -p '8883:8883@loadbalancer' -p '1883:1883@loadbalancer'
ARCCLUSTERNAME := arc-akri-connector-004
STORAGEACCOUNTNAME := saakriconnector004
SCHEMAREGISTRYNAME := sr-akri-connector-004
DEVICEREGISTRYNAME := adr-akri-connector-004
RESOURCEGROUP := rg-akri-connector-004
LOCATION := westeurope
VERSION := $(shell grep "<ContainerImageTag>" ./src/akri-connector-sample/EventDrivenTcpConnector/EventDrivenTcpConnector.csproj | sed 's/[^0-9.]*//g')
VERSIONDEPRECATED := $(shell grep "<ContainerImageTag>" ./src/deprecated/TcpConnector/TcpConnector.csproj | sed 's/[^0-9.]*//g')

all: create_k3d_cluster deploy_aio deploy_device deploy_assets deploy_3p_connector deploy_mqttui

deprecated: infra deploy_assets_deprecated deploy_asset_endpoint_profile_deprecated deploy_3p_connector_deprecated

sample: infra deploy_assets_sample_deprecated deploy_asset_endpoint_profile_sample_deprecated deploy_3p_connector_sample_deprecated

infra: create_k3d_cluster deploy_aio deploy_rest_server install_aep_asset_crds install_akri_operator deploy_mqttui

create_k3d_cluster:
	@echo "Creating k3d cluster..."
	k3d cluster create $(K3DCLUSTERNAME) $(PORTFORWARDING) --servers 1

deploy_aio:
	@echo "Deploying AIO..."
	bash ./deploy/deploy-aio.sh $(ARCCLUSTERNAME) $(STORAGEACCOUNTNAME) $(SCHEMAREGISTRYNAME) $(RESOURCEGROUP) $(LOCATION) $(DEVICEREGISTRYNAME)

deploy_rest_server:
	@echo "Deploying REST Server"
	helm install rest-server oci://akribuilds.azurecr.io/helm/rest-server --version 0.1.7 -n azure-iot-operations

install_aep_asset_crds:
	@echo "Installing AssetEndpointProfile and Asset CRDs"
	helm install adr --version 1.0.0 oci://mcr.microsoft.com/azureiotoperations/helm/adr/assets-arc-extension -n azure-iot-operations

install_akri_operator:
	@echo "Installing AKRI Operator"
	helm install akri-operator oci://akripreview.azurecr.io/helm/microsoft-managed-akri-operator --version 0.1.5-preview -n azure-iot-operations

deploy_device:
	@echo "Deploying Device"
	kubectl apply -f ./deploy/tcp-service-device-definition.yaml

deploy_assets:
	@echo "Deploying Assets"
	kubectl apply -f ./deploy/tcp-service-asset-definition.yaml

deploy_asset_endpoint_profile_deprecated:
	@echo "Deploying AssetEndpointProfile"
	kubectl apply -f ./deploy/tcp-asset-endpoint-profile-definition.yaml

deploy_asset_endpoint_profile_sample_deprecated:
	@echo "Deploying AssetEndpointProfile"
	kubectl apply -f ./deploy/rest-server-asset-endpoint-profile-definition.yaml

deploy_assets_deprecated:
	@echo "Deploying Assets"
	kubectl apply -f ./deploy/tcp-asset-definition.yaml

deploy_assets_sample_deprecated:
	@echo "Deploying Assets"
	kubectl apply -f ./deploy/rest-server-asset-definition.yaml

deploy_3p_connector: build_3p_connector_image deploy_3p_connector_template

deploy_3p_connector_deprecated: build_3p_connector_image_deprecated deploy_3p_connector_config_deprecated

deploy_3p_connector_sample_deprecated: deploy_3p_connector_config_sample_deprecated

build_3p_connector_image_deprecated:
	@echo "Building 3p Connector Image"
	docker build . -f ./src/deprecated/TcpConnector/Dockerfile -t tcpconnector:$(VERSIONDEPRECATED)
	k3d image import tcpconnector:$(VERSIONDEPRECATED) -c $(K3DCLUSTERNAME)

deploy_3p_connector_config_deprecated:
	@echo "Deploying 3p Connector Config"
	# on a mac (sed -i '' "s?__{image_version}__?$(VERSIONDEPRECATED)?g" ./deploy/connector-config.yaml)
	sed -i '' "s?__{image_version}__?$(VERSIONDEPRECATED)?g" ./deploy/connector-config.yaml
	kubectl apply -f ./deploy/connector-config.yaml

deploy_3p_connector_config_sample_deprecated:
	@echo "Deploying 3p Connector Config Sample"
	kubectl apply -f ./deploy/connector-config-sample.yaml

build_3p_connector_image:
	@echo "Building 3p Connector Image"
	docker build . -f ./src/deprecated/TcpConnector/Dockerfile -t eventdriventcpconnector:$(VERSION)
	k3d image import eventdriventcpconnector:$(VERSION) -c $(K3DCLUSTERNAME)

deploy_3p_connector_template:
	@echo "Deploying 3p Connector Template"
	# on a mac (sed -i '' "s?__{image_version}__?$(VERSION)?g" ./deploy/connector-template.yaml)
	sed -i '' "s?__{image_version}__?$(VERSION)?g" ./deploy/connector-template.yaml
	kubectl apply -f ./deploy/connector-template.yaml

deploy_mqttui:
	@echo "Deploying MQTT UI"
	kubectl apply -f ./deploy/mqttclient.yaml

clean:
	@echo "Cleaning up..."
	k3d cluster delete $(K3DCLUSTERNAME)