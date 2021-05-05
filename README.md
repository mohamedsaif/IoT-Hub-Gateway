# IoT-Hub-Gateway
Cloud native IoT Hub Gateway implementation to support devices that can't connect directly to IoT Hub supported protocols

# Azure Functions

Currently the project is developed using version 3.0 of Azure Functions (Azure Functions Core Tools v3.0.3442)

```bash

# Installing the tools on Ubuntu
sudo apt-get install azure-functions-core-tools-3

```

To manually build the docker file locally you can use:

```bash
docker build -t gateway-orchestrator:v1.0.0 .
```

To generate Kubernetes deployment file, you can also use (you need to replace the container registry url)

```bash

# This will generate ready to deploy manifest with all secrets and services for that particular function
func kubernetes deploy --name gateway-orchestrator --registry <container-registry-username> --dry-run

```