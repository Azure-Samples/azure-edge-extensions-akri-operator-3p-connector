# Context app for DSS deployment using the AKRI operator:

This sample uses the HTTP thermostat connector app from the iot-operations-sdks repository: https://github.com/Azure/iot-operations-sdks/tree/feat/operatorCompatibility/dotnet/samples/HttpThermostatConnectorApp

To run this sample, follow the below steps:

1. Deploy the MQ broker. Set up the broker to use SAT for authentication, and to use TLS for communication.
1. Deploy a Node.js service that will simulate the [Sample REST endpoint](./SampleHttpServer/ReadMe.md)
1. The httpthermostatconnectorapp image has been built and pushed to `akribuilds`: `akribuilds/samples/httpthermostatconnectorapp:0.1.12`.  

    Alternatively, build and push the container image to your cluster using the following command with:  
    NuGet.config (HttpThermostatConnectorApp)  
    ```
    <?xml version="1.0" encoding="utf-8"?>
    <configuration>
    <packageSources>
        <clear />
        <add key="nuget.org" value="https://api.nuget.org/v3/index.json"/>
        <add key="preview" value="https://pkgs.dev.azure.com/azure-iot-sdks/iot-operations/_packaging/preview/nuget/v3/index.json" />
    </packageSources>
    
    <packageSourceMapping>
        <!-- Use nuget.org to source all packages other than the unreleased Azure.Iot.Operations packages -->
        <packageSource key="nuget.org">
        <package pattern="*" />
        </packageSource>
        <packageSource key="preview">
        <package pattern="Azure.Iot.Operations.*" />
        </packageSource>
    </packageSourceMapping>
    </configuration>
    ```
    `dotnet publish /t:PublishContainer`   
    `k3d image import httpthermostatconnectorapp:0.1.12 -c myClusterMq1`  

    Whichever approach you use, ensure that the correct image is specified in your ConnectorConfig.
    This version of ConnectorConfig in this repo uses the image `akribuilds.azurecr.io/samples/httpthermostatconnectorapp:0.1.12`.
1. Create the ConnectorConfig and AEP instance yaml files. Samples available [here](./KubernetesResources/). These instances will be monitored by the AKRI operator and will be used for workload application lifetime management. 
Of note:
    1. The ConnectorConfig image field should specify the image name that was created in the above step.
    1. The AEP instance should specify the correct credentials required to connect to the HTTP and/or SQL endpoints deployed above.
1. Run the akri operator
1. Apply the ConnectorConfig and AEP instances created above.  

**Detailed steps are available in the [AKRI operator ReadMe](../../../operator/ReadMe.md)**

Updating the image:
1. `az login`   
   `az acr login --name akribuilds`
1. `dotnet publish /t:PublishContainer`
1. `docker tag httpthermostatconnectorapp:0.1.12 akribuilds.azurecr.io/samples/httpthermostatconnectorapp:0.1.12`
1. `docker push akribuilds.azurecr.io/samples/httpthermostatconnectorapp:0.1.12`