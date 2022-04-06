# You need to have:
# AKS target cluster set as the current kubectl context
# AKS should have Dapr and KEDA installed (refer to the README)
# All Azure dependent services are provisioned and the connection keys are ready

# Azure Container Registry name
ACR_NAME=REPLACE

# Make sure that the bash command line folder context set to the root where dockerfile exists

################
# Orchestrator #
################

# Container build and push
az acr build -t iothub/gateway-orchestrator:{{.Run.ID}} -t iothub/gateway-orchestrator:latest -r $ACR_NAME .

# Update the following files:
# deployment.yaml: replace the ACR name
# deployment-secrets.yaml: connectionString: with service bus connection string and APPINSIGHTS_CONNECTIONSTRING: connection string

# Adding services
kubectl apply -f ./Deployments/components
kubectl apply -f ./Deployments

# Validate
kubectl get pod,svc -n iot-hub-gateway

# Test the APIs
kubectl port-forward -n iot-hub-gateway service/gateway-orchestrator-http-service 8080:80
curl http://localhost:8080/api/GatewayOrchestrator/version

##############
# Translator #
##############

# Container build and push
az acr build -t iothub/gateway-translator:{{.Run.ID}} -t iothub/gateway-translator:latest -r $ACR_NAME .

# Update the following files:
# deployment.yaml: replace the ACR name
# deployment-secrets.yaml: connectionString: with service bus connection string and APPINSIGHTS_CONNECTIONSTRING: connection string

# Adding services
kubectl apply -f ./Deployments/components
kubectl apply -f ./Deployments

# Validate
kubectl get pod,svc -n iot-hub-gateway

# Testing the API (via the pod as there is no service)
WORKLOAD_POD=$(kubectl get pods -n iot-hub-gateway -l app=gateway-translator-sb -o jsonpath='{.items[0].metadata.name}')
echo $WORKLOAD_POD
kubectl port-forward -n iot-hub-gateway pod/$WORKLOAD_POD 8081:80
curl http://localhost:8080/api/GatewayTranslator/version

##################
# Gateway Server #
##################

# Container build and push
az acr build -t iothub/gateway-server:{{.Run.ID}} -t iothub/gateway-server:latest -r $ACR_NAME .

# Update the following files:
# deployment.yaml: replace the ACR name
# deployment-secrets.yaml: connectionString: with IoT Hub connection string and APPINSIGHTS_CONNECTIONSTRING: connection string

# Adding services
kubectl apply -f ./Deployments

# Validate
kubectl get pod,svc -n iot-hub-gateway

# Test the APIs
kubectl port-forward service/gateway-server-http-service 8082:80 -n iot-hub-gateway
curl http://localhost:8082/api/gateway

##########
# Zipkin #
##########

# Making sure it is deployed
kubectl create deployment zipkin --image openzipkin/zipkin -n dapr-system
kubectl expose deployment zipkin --type ClusterIP --port 9411 -n dapr-system

kubectl port-forward service/zipkin 8083:9411 -n dapr-system
echo http://localhost:8083