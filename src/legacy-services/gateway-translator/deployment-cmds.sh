#################################################
# First: container deployment to registry       #
#################################################
# Azure Container Registry name
ACR_NAME=REPLACE

# Make sure that the bash command line folder context set to the root where dockerfile exists

# OPTION 1: Using ACR Tasks
# With dynamic version
az acr build -t iothub/gateway-translator:{{.Run.ID}} -t iothub/gateway-translator:latest -r $ACR_NAME .

# With static tag only
# az acr build -t iothub/gateway-translator:1.0.0 -r $ACR_NAME .

# Using custom docker files
# az acr build -t iothub/gateway-translator:1.0.2 -r $ACR_NAME . --file Dockerfile-IotHubServer

# OPTION 2: Using docker tools (mainly for local testing)
ACR_SERVER=REPLACE.azurecr.io
ACR_USER=REPLACE
ACR_PASSWORD=REPLACE
docker login $ACR_SERVER -u $ACR_USER -p $ACR_PASSWORD 

# Docker build locally
docker build -t gateway-translator:1.0.0 .

# Optional: Test locally
docker run -d -p 8080:80 --name gateway-translator \
  -e "gateway-translator-sb-conn=REPLACE" \
  -e "gateway-server-host=REPLACE" \
  -e "FUNCTIONS_WORKER_RUNTIME=dotnet" \
  -e "AzureWebJobsStorage=UseDevelopmentStorage=true" \
  gateway-translator:1.0.0

# Docker tag & push to ACR
docker tag gateway-translator:1.0.0 $ACR_SERVER/iothub/gateway-translator:1.0.0

docker push $ACR_SERVER/iothub/gateway-translator:1.0.0

##########################################
# Second: Deployment to Kubernetes       #
##########################################

# Handling the secrets if you want to use the provided -secrets.yaml file
# You need to encode the literal value to base64 before adding it to the file
# Note: Service bus connection string needs to have receive permission and DON'T include the "EntityPath"
# Note: KEDA service bus connection string needs to be with manage permission and include the "EntityPath"
echo -n "REPLACE_FUNC_SERVICE_BUS_CONNECTION" | base64 -w 0
echo -n "REPLACE_KEDA_SERVICE_BUS_CONNECTION" | base64 -w 0
echo -n "REPLACE_APPINSIGHTS_INSTRUMENTATIONKEY" | base64 -w 0

cd Deployment

# Namespace deployment is optional if the namespace already exists
kubectl apply -f deployment-namespace.yaml
# Update the secrets with the required base64 values using the command above
kubectl apply -f deployment-secrets.yaml
kubectl apply -f deployment.yaml
# Apply this only after installing KEDA
kubectl apply -f deployment-keda.yaml

# Check the deployed resources
kubectl get all -n iot-hub-gateway
# Check the KEDA scaled object
kubectl get scaledobject -n iot-hub-gateway

# Diagnotics tips
kubectl logs -n iot-hub-gateway -f pod/gateway-translator-sb-REPLACE-RANDOM
kubectl describe -n iot-hub-gateway pod/gateway-translator-sb-REPLACE-RANDOM
kubectl exec -it -n iot-hub-gateway pod/gateway-translator-sb-REPLACE-RANDOM /bin/bash