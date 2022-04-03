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
kubectl apply -f ./Deployments

# Validate
kubectl get pod,svc -n iot-hub-gateway

##############
# Translator #
##############

# Container build and push
az acr build -t iothub/gateway-translator:{{.Run.ID}} -t iothub/gateway-translator:latest -r $ACR_NAME .

# Update the following files:
# deployment.yaml: replace the ACR name
# deployment-secrets.yaml: connectionString: with service bus connection string and APPINSIGHTS_CONNECTIONSTRING: connection string

# Adding services
kubectl apply -f ./Deployments

# Validate
kubectl get pod,svc -n iot-hub-gateway

##################
# Gateway Server #
##################

