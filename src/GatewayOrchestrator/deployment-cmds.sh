#################################################
# First: container deployment to registry       #
#################################################
# Azure Container Registry name
ACR_NAME=REPLACE

# Make sure that the bash command line folder context set to the root where dockerfile exists

# OPTION 1: Using ACR Tasks
# With dynamic version
az acr build -t iothub/gateway-orchestrator:{{.Run.ID}} -t iothub/gateway-orchestrator:latest -r $ACR_NAME .

# With static tag only
# az acr build -t iothub/gateway-orchestrator:1.0.0 -r $ACR_NAME .

# Using custom docker files
# az acr build -t iothub/gateway-orchestrator:1.0.2 -r $ACR_NAME . --file Dockerfile-IotHubServer

# OPTION 2: Using docker tools (mainly for local testing)
ACR_SERVER=REPLACE.azurecr.io
ACR_USER=REPLACE
ACR_PASSWORD=REPLACE
docker login $ACR_SERVER -u $ACR_USER -p $ACR_PASSWORD 

# Docker build locally
docker build -t gateway-orchestrator:1.0.0 .

# Optional: Test locally
docker run -d -p 8080:80 --name gateway-orchestrator \
  -e "IoTHubHostName=REPLACE.azure-devices.net" \
  -e "AccessPolicyName=REPLACE" \
  -e "AccessPolicyKey=REPLACE" \
  -e "SharedAccessPolicyKeyEnabled=true" \
  -e "DirectMethodEnabled=true" \
  -e "CloudMessagesEnabled=true" \
  gateway-orchestrator:1.0.0

# Docker tag & push to ACR
docker tag gateway-orchestrator:1.0.0 $ACR_SERVER/iothub/gateway-orchestrator:1.0.0

docker push $ACR_SERVER/iothub/gateway-orchestrator:1.0.0

##########################################
#####        Dapr local run        #######
##########################################

# making sure dapr is installed and initialized
dapr -v

# To run both the app and dapr side-car at the same, I'm using docker-compose file
docker compose up

# dapr run --app-id gateway-orchestrator --components-path ./DaprComponentsDev dotnet run

##########################################
# Second: Deployment to Kubernetes       #
##########################################

# Handling the secrets if you want to use the provided -secrets.yaml file
# You need to encode the literal value to base64 before adding it to the file
# Note: Service bus connection string needs to have receive permission and DON'T include the "EntityPath"
# Replace both REPLACE_SERVICE_BUS_CONNECTION and REPLACE_APPINSIGHTS_CONNECTIONSTRING with relevant values
echo -n "InstrumentationKey=REPLACE;IngestionEndpoint=https://REPLACE.applicationinsights.azure.com/" | base64 -w 0
echo -n "Endpoint=sb://REPLACE.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=REPALCE" | base64 -w 0
cd Deployment

# Namespace deployment is optional if the namespace already exists
kubectl apply -f deployment-namespace.yaml
# Update the secrets with the required base64 values using the command above
kubectl apply -f deployment-secrets.yaml

kubectl apply -f deployment-configmap.yaml

# Dapr components
kubectl apply -f components/azure-servicebus-component.yaml

kubectl apply -f deployment.yaml
kubectl apply -f deployment-service.yaml


# Check the deployed resources
kubectl get all -n iot-hub-gateway

# Testing
# Using public IP: get the service IP
SERVICE_EXTERNAL_IP=$(kubectl get service \
    gateway-orchestrator-http-service \
    -n iot-hub-gateway \
    -o jsonpath='{.status.loadBalancer.ingress[0].ip}')
echo $SERVICE_EXTERNAL_IP
curl http://$SERVICE_EXTERNAL_IP/api/GatewayOrchestrator/version

# Using port-forward
kubectl port-forward -n iot-hub-gateway service/gateway-orchestrator-http-service 8080:80
curl http://localhost:8080/api/GatewayOrchestrator/version

# Diagnotics tips
WORKLOAD_POD=$(kubectl get pods -n iot-hub-gateway -l app=gateway-orchestrator-http -o jsonpath='{.items[0].metadata.name}')
echo $WORKLOAD_POD
kubectl logs -n iot-hub-gateway $WORKLOAD_POD -c gateway-orchestrator-http
kubectl logs -n iot-hub-gateway $WORKLOAD_POD -c daprd

# Diagnostics tips
kubectl logs -n iot-hub-gateway -f pod/gateway-orchestrator-http-REPLACE-RANDOM
kubectl describe -n iot-hub-gateway pod/gateway-orchestrator-http-REPLACE-RANDOM
kubectl exec -it -n iot-hub-gateway pod/gateway-orchestrator-http-REPLACE-RANDOM /bin/bash

kubectl logs -n iot-hub-gateway gateway-orchestrator-http-REPLACE-RANDOM -c daprd
kubectl logs -n iot-hub-gateway gateway-orchestrator-http-REPLACE-RANDOM -c gateway-orchestrator-http
kubectl describe po -n iot-hub-gateway gateway-orchestrator-http-REPLACE-RANDOM