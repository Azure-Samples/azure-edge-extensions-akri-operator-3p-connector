#!/bin/bash
scriptdir="$(dirname "$0")"
cd "$scriptdir"

k3d cluster delete --all
k3d cluster create myClusterMq1 -p '1883:1883@loadbalancer' -p '8883:8883@loadbalancer'
kubectl config set-context k3d-myClusterMq1 --namespace=azure-iot-operations
az extension add --upgrade --name azure-iot-ops
helm repo add jetstack https://charts.jetstack.io --force-update
helm upgrade cert-manager jetstack/cert-manager --install --create-namespace -n cert-manager --version v1.16 --set crds.enabled=true --set extraArgs={--enable-certificate-owner-ref=true} --wait
helm upgrade trust-manager jetstack/trust-manager --install --create-namespace -n cert-manager --wait
helm uninstall mq --ignore-not-found
helm uninstall broker --ignore-not-found
helm install broker --atomic --create-namespace -n azure-iot-operations --version 0.7.11 oci://mcr.microsoft.com/azureiotoperations/helm/aio-broker --values ./broker-values.yaml --wait
kubectl wait --for=create --timeout=30s secret/azure-iot-operations-aio-ca-certificate -n cert-manager
kubectl apply -f ./broker-consolidated.yaml
