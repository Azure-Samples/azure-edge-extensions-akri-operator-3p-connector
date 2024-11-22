TLDR;  
`helm install http-server oci://akribuilds.azurecr.io/helm/http-server --version 0.1.2`
> Make a note of the secret section and service section of `http-server-helm/values.yaml`. These will need to be specified in the Http Asset Endpoint Profile CR instance.

### Details:

1. The image has been built and pushed to `akribuilds`: `akribuilds/samples/sample-http-server:0.1.2`  
    
    Alternatively, build and push the container image to your cluster using the following command with:  
    ```dockerfile
    # Dockerfile
    FROM node:14

    WORKDIR /usr/src/app
    COPY package*.json ./
    RUN npm install
    COPY . .

    EXPOSE 80
    CMD ["node", "server.js"]                                                                           
    ```
    `docker build -t sample-http-server:0.1.2 -f Dockerfile .`  
    `k3d image import sample-http-server:0.1.2 -c myClusterMq1`  

    Whichever approach you use, ensure that the correct image is specified in `http-server.yaml`.
    This version of context-app-for-dss communicates with the endpoint from the image `akribuilds.azurecr.io/samples/sample-http-server:0.1.2`.

1. Choose username/password that the Node JS service will use to authenticate. Base 64 encode them and replace it in the secret named http-username and http-password in the http-server.yaml.  
    `echo -n "some-username" | base64`  
    `echo -n "some-password" | base64`  
    > The context-app-for-dss will use this username and password to authenticate with the Node.js service. 

1. Deploy the Node.js Service to Kubernetes by running the following command. Use the correct image name in the yaml file.  
    `kubectl apply -f http-server.yaml`

1. Verify that the Node.js Service is running by running the following command:  
    `kubectl get pods -A`

1. On the logs of the pod, the following message should be seen:  
    ```bash
    kubectl logs -l app=http-server-deployment --all-containers=true --since=0s --tail=-1 --max-log-requests=1
    Server listening on port 80
    ```

Updating the image and the helm chart:
1. `az login`   
   `az acr login --name akribuilds`
1. `docker build -t sample-http-server:0.1.2 -f Dockerfile .`
1. `docker tag sample-http-server:0.1.2 akribuilds.azurecr.io/samples/sample-http-server:0.1.2`
1. `docker push akribuilds.azurecr.io/samples/sample-http-server:0.1.2`
1. From the folder `helm`  
1. Update the image reference in `values.yaml` -> `deployment.image`
   `helm package .`
1. `helm push http-server-0.1.2.tgz oci://akribuilds.azurecr.io/helm/`
