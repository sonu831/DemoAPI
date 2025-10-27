# Kubernetes Deployment Guide
## Student Management API with SQL Server

This guide explains how to manually deploy a .NET Web API with SQL Server database to Kubernetes (Docker Desktop).

---

## Table of Contents
- [Prerequisites](#prerequisites)
- [Project Structure](#project-structure)
- [Initial Setup](#initial-setup)
- [Deployment Steps](#deployment-steps)
- [Accessing Services](#accessing-services)
- [Verification](#verification)
- [Troubleshooting](#troubleshooting)
- [Useful Commands](#useful-commands)
- [Cleanup](#cleanup)

---

## Important Note on Directory Structure

This project has a nested structure:
- **Solution Root:** `SampleWebAPI/` - Contains `.env`, `k8s-deployment.yaml`, `README.md`
- **Project Root:** `SampleWebAPI/SampleWebAPI/` - Contains `Dockerfile`, `Controllers/`, `Program.cs`, etc.

**All kubectl and setup commands** should be run from the **Solution Root** directory.
**Docker build** is run from the **Project Root** where the Dockerfile is located.

---

## Prerequisites

### Required Software
- **Docker Desktop** with Kubernetes enabled
- **kubectl** CLI tool (comes with Docker Desktop)
- **SQL Server Management Studio (SSMS)** or Azure Data Studio
- **.NET 8.0 SDK** (for building the Web API image)
- **Git** (optional, for version control)

### Verify Kubernetes is Running
```bash
# Check kubectl is installed
kubectl version --client

# Check Kubernetes cluster is running
kubectl cluster-info

# Check nodes are ready
kubectl get nodes
```

You should see your Docker Desktop nodes in `Ready` status.

---

## Project Structure

```
SampleWebAPI/                   # Solution root
├── .env                        # Environment variables (DO NOT commit to Git!)
├── .gitignore                  # Git ignore file (includes .env)
├── k8s-deployment.yaml         # Kubernetes manifests
├── README.md                   # This file
├── SampleWebAPI.sln            # Visual Studio solution file (optional)
└── SampleWebAPI/               # Project folder
    ├── Dockerfile              # Web API Docker image
    ├── Controllers/            # API Controllers
    ├── Program.cs              # Application entry point
    ├── appsettings.json        # App configuration
    └── [Other .NET project files]
```

---

## Initial Setup

**Working Directory:** All commands in this guide should be run from the **solution root** (`SampleWebAPI/` folder), unless otherwise specified.

```bash
# Verify you're in the correct directory
pwd
# Should show: /path/to/SampleWebAPI (solution root)

# You should see these files/folders:
ls
# .env  k8s-deployment.yaml  README.md  SampleWebAPI/
```

### 1. Create .env File

Create a `.env` file in your **solution root** directory:

```bash
DB_USER=sa
DB_PASSWORD=Admin@123456
ASPNETCORE_ENVIRONMENT=Development
```

**Important:** Replace `Admin@123456` with your desired SQL Server password. It must meet SQL Server requirements:
- At least 8 characters
- Contains uppercase and lowercase letters
- Contains numbers
- Contains special characters

### 2. Add .env to .gitignore

Ensure `.env` is in your `.gitignore` file to prevent committing passwords:

```bash
# Add to .gitignore
echo ".env" >> .gitignore
```

### 3. Build Your Web API Docker Image

```bash
# Navigate to the project directory where Dockerfile is located
cd SampleWebAPI/SampleWebAPI

# Build the Docker image for your Web API
docker build -t my-webapi:latest17 .

# Go back to solution root
cd ../..

# Verify the image was created
docker images | grep my-webapi
```

**Note:** The image name `my-webapi:latest17` must match what's in `k8s-deployment.yaml`.

**Alternative:** If you want to build from the solution root without changing directories:
```bash
# Build from solution root, specifying the context path
docker build -t my-webapi:latest17 -f SampleWebAPI/SampleWebAPI/Dockerfile SampleWebAPI/SampleWebAPI
```

---

## Deployment Steps

Follow these steps in order to deploy your application to Kubernetes.

### Step 1: Create Kubernetes Secret from .env File

```bash
# Delete existing secret if it exists
kubectl delete secret webapi-secret --ignore-not-found

# Create secret from .env file
kubectl create secret generic webapi-secret --from-env-file=.env

# Verify secret was created
kubectl get secret webapi-secret
kubectl describe secret webapi-secret
```

### Step 2: Create Service Account and RBAC

```bash
# Apply the service account and RBAC configuration
kubectl apply -f - <<EOF
apiVersion: v1
kind: ServiceAccount
metadata:
  name: webapi-serviceaccount
  namespace: default
---
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRole
metadata:
  name: webapi-cluster-reader
rules:
- apiGroups: [""]
  resources: ["pods", "services", "nodes"]
  verbs: ["get", "list"]
- apiGroups: ["apps"]
  resources: ["deployments", "replicasets"]
  verbs: ["get", "list"]
---
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRoleBinding
metadata:
  name: webapi-cluster-reader-binding
subjects:
- kind: ServiceAccount
  name: webapi-serviceaccount
  namespace: default
roleRef:
  kind: ClusterRole
  name: webapi-cluster-reader
  apiGroup: rbac.authorization.k8s.io
EOF
```

### Step 3: Deploy Application

```bash
# Apply all Kubernetes resources
kubectl apply -f k8s-deployment.yaml

# Watch the deployment progress
kubectl get pods -w
```

Press `Ctrl+C` to stop watching once all pods show `Running` status.

### Step 4: Wait for SQL Server to Initialize

SQL Server takes about 60-90 seconds to fully initialize. Monitor the logs:

```bash
# Watch SQL Server logs
kubectl logs -f -l app=sqlserver
```

Wait until you see:
```
Recovery is complete. This is an informational message only. No user action is required.
```

Press `Ctrl+C` to exit the logs.

### Step 5: Verify All Pods are Running

```bash
# Check pod status
kubectl get pods

# Expected output:
# NAME                         READY   STATUS    RESTARTS   AGE
# sqlserver-xxxxx-xxxxx        1/1     Running   0          2m
# webapi-xxxxx-xxxxx           1/1     Running   0          2m
# webapi-xxxxx-xxxxx           1/1     Running   0          2m
```

All pods should show `1/1` under READY and `Running` status.

### Step 6: Verify Services

```bash
# Check services
kubectl get svc

# Expected output:
# NAME                 TYPE           CLUSTER-IP      EXTERNAL-IP   PORT(S)
# kubernetes           ClusterIP      10.96.0.1       <none>        443/TCP
# sqlserver-service    LoadBalancer   10.96.x.x       localhost     1433:xxxxx/TCP
# webapi-service       LoadBalancer   10.96.x.x       localhost     5052:xxxxx/TCP
```

With Docker Desktop, LoadBalancer services automatically expose on `localhost`.

---

## Accessing Services

### SQL Server

**Using SQL Server Management Studio (SSMS):**
- **Server name:** `localhost,1433` (note the comma)
- **Authentication:** SQL Server Authentication
- **Login:** `sa`
- **Password:** `Admin@123456` (or whatever you set in .env)

**Using sqlcmd:**
```bash
sqlcmd -S localhost,1433 -U sa -P Admin@123456 -Q "SELECT @@VERSION"
```

### Web API

**Swagger UI:**
```
http://localhost:5052/swagger/index.html
```

**System Info Endpoint:**
```bash
curl http://localhost:5052/api/systeminfo
```

**Students API:**
```
http://localhost:5052/api/students
```

---

## Verification

### 1. Check All Resources

```bash
# View all resources
kubectl get all

# Check ConfigMaps
kubectl get configmap

# Check Secrets
kubectl get secret

# Check PersistentVolumeClaims
kubectl get pvc
```

### 2. Test Database Connection

Check the system info endpoint to verify database connectivity:

```bash
curl http://localhost:5052/api/systeminfo | jq .databaseStatus
```

Should return: `"Connected"` or connection details, not an error.

### 3. View Logs

**SQL Server logs:**
```bash
kubectl logs -l app=sqlserver --tail=50
```

**Web API logs:**
```bash
kubectl logs -l app=webapi --tail=50
```

**Follow logs in real-time:**
```bash
kubectl logs -f deployment/webapi
```

### 4. Test the API

**Create a student:**
```bash
curl -X POST http://localhost:5052/api/students \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "John",
    "lastName": "Doe",
    "email": "john.doe@example.com",
    "dateOfBirth": "2000-01-15"
  }'
```

**Get all students:**
```bash
curl http://localhost:5052/api/students
```

---

## Troubleshooting

### Pods Not Starting

**Check pod status:**
```bash
kubectl get pods
kubectl describe pod <pod-name>
```

**Check events:**
```bash
kubectl get events --sort-by='.lastTimestamp'
```

### SQL Server Connection Issues

**Issue:** `Cannot connect to localhost,1433`

**Solution 1 - Check if SQL Server is running:**
```bash
kubectl get pods -l app=sqlserver
kubectl logs -l app=sqlserver --tail=100
```

**Solution 2 - SQL Server still initializing:**
Wait 60-90 seconds after pod starts, then try again.

**Solution 3 - Wrong password:**
```bash
# Verify password in secret
kubectl get secret webapi-secret -o jsonpath='{.data.DB_PASSWORD}' | base64 -d
echo ""

# If wrong, recreate the secret
kubectl delete secret webapi-secret
kubectl create secret generic webapi-secret --from-env-file=.env

# Restart pods
kubectl rollout restart deployment sqlserver
kubectl rollout restart deployment webapi
```

### Database Already Initialized with Different Password

**Issue:** `Login failed for user 'sa'`

**Solution:** Delete PVC and recreate with new password:
```bash
# Delete SQL Server deployment and persistent volume
kubectl delete deployment sqlserver
kubectl delete pvc sqlserver-pvc

# Recreate secret with correct password
kubectl delete secret webapi-secret
kubectl create secret generic webapi-secret --from-env-file=.env

# Redeploy
kubectl apply -f k8s-deployment.yaml
```

### Web API Cannot Reach SQL Server

**Check service endpoints:**
```bash
kubectl get endpoints sqlserver-service
```

Should show the SQL Server pod IP. If empty, the service selector might be wrong.

**Test from within a pod:**
```bash
# Execute into Web API pod
kubectl exec -it deployment/webapi -- /bin/bash

# Test DNS resolution
nslookup sqlserver-service

# Test connectivity
curl telnet://sqlserver-service:1433
```

### Image Pull Errors

**Issue:** `ImagePullBackOff` or `ErrImagePull`

**Solution:**
```bash
# Verify image exists locally
docker images | grep my-webapi

# If not found, rebuild from the project directory
cd SampleWebAPI/SampleWebAPI
docker build -t my-webapi:latest17 .
cd ../..

# Ensure imagePullPolicy is IfNotPresent in YAML
```

**Common Docker Build Issues:**
- Make sure you're in the correct directory where Dockerfile exists
- Verify Dockerfile has correct path to .csproj file
- Check for any syntax errors in Dockerfile

### Pod CrashLoopBackOff

**Check logs for errors:**
```bash
kubectl logs <pod-name>
kubectl logs <pod-name> --previous  # Logs from previous crash
```

**Common causes:**
- SQL Server: Not enough memory allocated
- Web API: Cannot connect to database, check connection string

---

## Useful Commands

### Quick Reference Card

```bash
# From Solution Root (SampleWebAPI/)
kubectl apply -f k8s-deployment.yaml          # Deploy to Kubernetes
kubectl get pods                               # Check pod status
kubectl logs -l app=webapi                     # View Web API logs
kubectl create secret generic webapi-secret --from-env-file=.env  # Create secret

# Build Docker Image (Option 1: Navigate to project folder)
docker build -t my-webapi:latest17 -f SampleWebAPI/Dockerfile .
cd ../..

# Build Docker Image (Option 2: From solution root)
docker build -t my-webapi:latest17 -f SampleWebAPI/SampleWebAPI/Dockerfile SampleWebAPI/SampleWebAPI
```

### Viewing Resources
```bash
# Get all pods
kubectl get pods

# Get pods with more details
kubectl get pods -o wide

# Get specific deployment
kubectl get deployment sqlserver

# Get services
kubectl get svc

# Get all resources
kubectl get all
```

### Logs and Debugging
```bash
# View logs
kubectl logs <pod-name>

# Follow logs
kubectl logs -f <pod-name>

# View logs for all pods with label
kubectl logs -l app=webapi --tail=50

# Execute into a pod
kubectl exec -it <pod-name> -- /bin/bash

# Port forward to local machine
kubectl port-forward pod/<pod-name> 8080:8080
```

### Updating Resources
```bash
# Apply changes
kubectl apply -f k8s-deployment.yaml

# Restart a deployment
kubectl rollout restart deployment/webapi

# Scale a deployment
kubectl scale deployment webapi --replicas=3

# Check rollout status
kubectl rollout status deployment/webapi
```

### Describing Resources
```bash
# Describe pod (shows events and details)
kubectl describe pod <pod-name>

# Describe service
kubectl describe svc webapi-service

# Describe node
kubectl describe node <node-name>
```

---

## Cleanup

### Delete All Resources

**Option 1: Delete by file**
```bash
kubectl delete -f k8s-deployment.yaml
```

**Option 2: Delete individually**
```bash
# Delete deployments
kubectl delete deployment sqlserver webapi

# Delete services
kubectl delete service sqlserver-service webapi-service

# Delete ConfigMap
kubectl delete configmap webapi-config

# Delete Secret
kubectl delete secret webapi-secret

# Delete PVC (this will delete database data!)
kubectl delete pvc sqlserver-pvc

# Delete Service Account and RBAC
kubectl delete serviceaccount webapi-serviceaccount
kubectl delete clusterrole webapi-cluster-reader
kubectl delete clusterrolebinding webapi-cluster-reader-binding
```

### Verify Cleanup

```bash
# Should show no resources
kubectl get all
kubectl get pvc
kubectl get configmap
kubectl get secret
```

---

## Configuration Details

### Environment Variables

**SQL Server Container:**
- `ACCEPT_EULA`: Accepts SQL Server license (required)
- `SA_PASSWORD`: SA user password (from Secret)

**Web API Container:**
- `ASPNETCORE_ENVIRONMENT`: Development/Production
- `DB_SERVER`: `sqlserver-service` (DNS name in cluster)
- `DB_NAME`: `StudentManagementDB`
- `DB_USER`: `sa`
- `DB_PASSWORD`: From Secret
- `POD_NAME`, `POD_NAMESPACE`, `POD_IP`, `NODE_NAME`: Injected by Kubernetes

### Resource Limits

**SQL Server:**
- Requests: 2Gi memory, 500m CPU
- Limits: 4Gi memory, 1000m CPU

**Web API:**
- Requests: 128Mi memory, 100m CPU
- Limits: 512Mi memory, 500m CPU

### Health Checks

**SQL Server:**
- Readiness Probe: TCP check on port 1433, starts after 30s
- Liveness Probe: TCP check on port 1433, starts after 60s

**Web API:**
- Readiness Probe: HTTP GET `/api/systeminfo`, starts after 30s
- Liveness Probe: HTTP GET `/api/systeminfo`, starts after 60s

---

## Architecture Overview

```
┌─────────────────────────────────────────┐
│          LoadBalancer Services          │
│  ┌──────────────┐    ┌──────────────┐  │
│  │   SQL Server │    │   Web API    │  │
│  │ localhost:1433│    │localhost:5052│  │
│  └──────────────┘    └──────────────┘  │
└─────────────────────────────────────────┘
           ↓                    ↓
┌─────────────────────────────────────────┐
│            Deployments                  │
│  ┌──────────────┐    ┌──────────────┐  │
│  │  sqlserver   │    │    webapi    │  │
│  │  (1 replica) │    │  (2 replicas)│  │
│  └──────────────┘    └──────────────┘  │
└─────────────────────────────────────────┘
           ↓                    ↓
┌─────────────────────────────────────────┐
│               Pods                      │
│  ┌──────────────┐    ┌──────────────┐  │
│  │  sqlserver   │    │   webapi-1   │  │
│  │    pod       │    │              │  │
│  │              │    ├──────────────┤  │
│  │  [Database]  │←───│   webapi-2   │  │
│  │              │    │              │  │
│  └──────────────┘    └──────────────┘  │
└─────────────────────────────────────────┘
           ↓
┌─────────────────────────────────────────┐
│      PersistentVolumeClaim              │
│  ┌──────────────────────────────────┐  │
│  │     sqlserver-pvc (2Gi)          │  │
│  │   Stores database files          │  │
│  └──────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

---

## Security Best Practices

1. **Never commit `.env` file** - Always add to `.gitignore`
2. **Use strong passwords** - Follow SQL Server password requirements
3. **Rotate secrets regularly** - Update passwords periodically
4. **Use RBAC** - Service account has minimal required permissions
5. **Network policies** - Consider adding NetworkPolicy for pod-to-pod communication
6. **Use namespaces** - Separate dev/staging/prod environments
7. **Scan images** - Use `docker scan` to check for vulnerabilities

---

## Production Considerations

For production deployments, consider:

1. **Use External Database** - Azure SQL, AWS RDS, or managed SQL Server
2. **Use Ingress** - Instead of LoadBalancer for HTTP(S) routing
3. **Add TLS/SSL** - Use cert-manager for HTTPS
4. **Use Secrets Manager** - Azure Key Vault, AWS Secrets Manager, or HashiCorp Vault
5. **Add Monitoring** - Prometheus, Grafana, Application Insights
6. **Implement CI/CD** - GitHub Actions, Azure DevOps, Jenkins
7. **Use Helm Charts** - Package and version your Kubernetes manifests
8. **Add Backup Strategy** - Regular SQL Server backups to cloud storage
9. **Resource Quotas** - Set namespace resource limits
10. **Pod Security Policies** - Enforce security standards

---

## Support

For issues or questions:
- Check the [Troubleshooting](#troubleshooting) section
- Review Kubernetes logs: `kubectl logs -l app=<app-name>`
- Kubernetes Documentation: https://kubernetes.io/docs/
- Docker Desktop Documentation: https://docs.docker.com/desktop/

---

## License

[Your License Here]

---

## Contributors

[Your Name/Team]

---

**Last Updated:** October 2025
