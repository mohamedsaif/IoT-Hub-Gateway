#################################################
# First: container deployment to registry       #
#################################################
# Azure Container Registry name
ACR_NAME=REPLACE

# Make sure that the bash command line folder context set to the root where dockerfile exists

# OPTION 1: Using ACR Tasks
# With dynamic version
az acr build -t iothub/gateway-server:{{.Run.ID}} -t iothub/gateway-server:latest -r $ACR_NAME --file Dockerfile-aks .

# With static tag only
# az acr build -t iothub/gateway-server:1.0.0 -r $ACR_NAME .

# Using custom docker files
# az acr build -t iothub/gateway-server:1.0.2 -r $ACR_NAME --file Dockerfile-IotHubServer .

# OPTION 2: Using docker tools (mainly for local testing)
ACR_SERVER=REPLACE.azurecr.io
ACR_USER=REPLACE
ACR_PASSWORD=REPLACE
docker login $ACR_SERVER -u $ACR_USER -p $ACR_PASSWORD 

# Docker build locally
docker build -t gateway-server:1.0.0 .

# Optional: Test locally
docker run -d -p 8080:80 --name gateway-server \
  -e "IoTHubHostName=REPLACE.azure-devices.net" \
  -e "AccessPolicyName=REPLACE" \
  -e "AccessPolicyKey=REPLACE" \
  -e "SharedAccessPolicyKeyEnabled=true" \
  -e "DirectMethodEnabled=true" \
  -e "CloudMessagesEnabled=true" \
  gateway-server:1.0.0

# Docker tag & push to ACR
docker tag gateway-server:1.0.0 $ACR_SERVER/iothub/gateway-server:1.0.0

docker push $ACR_SERVER/iothub/gateway-server:1.0.0

##########################################
# Second: Deployment to Kubernetes       #
##########################################

# Handling the secrets if you want to use the provided -secrets.yaml file
# You need to encode the literal value to base64 before adding it to the file
echo -n "IoTHubHostName" | base64 -w 0
echo -n "AccessPolicyName" | base64 -w 0
echo -n "AccessPolicyKey" | base64 -w 0
echo -n "AppInsightsInstConnectionString" | base64 -w 0
cd Deployment

# Namespace deployment is optional if the namespace already exists
kubectl apply -f deployment-namespace.yaml
# Update the secrets with the required base64 values using the command above
kubectl apply -f deployment-secrets.yaml
kubectl apply -f deployment.yaml
kubectl apply -f deployment-service.yaml

# Check the deployed resources
kubectl get all -n iot-hub-gateway

# Test without a public IP (you can use Postman with localhost:6111/api/GatewayOrchestrator?deviceId=REPLACE-WITH-DEVICE-ID)
kubectl port-forward service/gateway-server-http-service 8082:80 -n iot-hub-gateway
curl http://localhost:8082/iot-gateway

# Using public IP: get the service IP
SERVICE_EXTERNAL_IP=$(kubectl get service \
    gateway-server-http-service \
    -n iot-hub-gateway \
    -o jsonpath='{.status.loadBalancer.ingress[0].ip}')
echo $SERVICE_EXTERNAL_IP
curl http://$SERVICE_EXTERNAL_IP/iot-gateway

# Diagnotics tips
WORKLOAD_POD=$(kubectl get pods -n iot-hub-gateway -l app=gateway-orchestrator-http -o jsonpath='{.items[0].metadata.name}')
echo $WORKLOAD_POD
kubectl logs -n iot-hub-gateway $WORKLOAD_POD -c gateway-orchestrator-http
kubectl logs -n iot-hub-gateway $WORKLOAD_POD -c daprd

# Diagnotics tips
kubectl logs -n iot-hub-gateway -f pod/gateway-server-http-REPLACE-RANDOM
kubectl describe -n iot-hub-gateway pod/gateway-server-http-REPLACE-RANDOM
kubectl exec -it -n iot-hub-gateway pod/gateway-server-http-REPLACE-RANDOM /bin/bash