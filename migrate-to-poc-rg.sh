#!/bin/bash
# =============================================================================
# CTB Supplier — Migrate to ctb-dev-poc-rg
#
# This script moves the container deployment and static outbound IP from
# ctb-dev-rg to ctb-dev-poc-rg.  The static outbound IP (52.187.226.64)
# is PRESERVED by moving the NAT Gateway and its Public IP rather than
# recreating them.
#
# ESTIMATED DOWNTIME: ~20-30 minutes (Phases 2-6).
#
# BEFORE RUNNING:
#   1. Fill in the two secret values on the lines marked FILL_IN_BELOW.
#      - CONNECTION_STRING : the database connection string (secret 'connstr')
#      - AAD_CLIENT_SECRET : the Azure AD client secret (secret 'aad-client-secret')
#   2. Update DNS records as described in the DNS CHANGES section at the
#      bottom of this file BEFORE running Phase 6.
#   3. Phase 1 (resource group creation) requires Owner or Contributor at
#      the Quantaco-CTB-Dev subscription level.  If rickp@quantaco.co
#      cannot do this, ask your Azure admin to create ctb-dev-poc-rg first,
#      then skip Phase 1.
#
# Run each phase individually so you can verify the output before proceeding.
# =============================================================================

set -euo pipefail

# Prevent Git Bash from converting /subscriptions/... paths to Windows paths
export MSYS_NO_PATHCONV=1

# ── Configuration ─────────────────────────────────────────────────────────────

SUBSCRIPTION="54c1a52b-07e4-4f9d-9683-4826e8a88cd4"
OLD_RG="ctb-dev-rg"
NEW_RG="ctb-dev-poc-rg"
LOCATION="australiaeast"
VNET_NAME="ctb-supplier-vnet"
SUBNET_NAME="ctb-supplier-subnet"
NATGW_NAME="ctb-supplier-natgw"
PIP_NAME="ctb-supplier-natgw-pip"
ACE_NAME="ctb-supplier-ace"
APP_NAME="ctb-supplier"
ACR_NAME="ctbdevacr"
ACR_SERVER="ctbdevacr.azurecr.io"
IMAGE="ctbdevacr.azurecr.io/ctb-supplier:v18"
CUSTOM_DOMAIN="supplier-poc.cookingthebooks.com.au"

# ── Secret values — FILL IN BEFORE RUNNING ────────────────────────────────────

CONNECTION_STRING="<FILL_IN_BELOW>"    # Was secret 'connstr'     — database connection string
AAD_CLIENT_SECRET="<FILL_IN_BELOW>"   # Was secret 'aad-client-secret' — Azure AD client secret


# =============================================================================
# PHASE 1 — Create new resource group
#
# Requires Owner or Contributor at subscription level.
# Skip this phase if an admin creates the RG for you.
# =============================================================================

phase1_create_resource_group() {
  echo ">>> Phase 1: Creating resource group $NEW_RG ..."
  az group create \
    --name "$NEW_RG" \
    --location "$LOCATION" \
    --subscription "$SUBSCRIPTION"
  echo ">>> Phase 1 complete."
}


# =============================================================================
# PHASE 2 — Delete old Container App and Environment
#
# THE SITE GOES DOWN here and stays down until Phase 6 completes.
#
# The subnet carries a serviceAssociationLink (allowDelete: false) that
# prevents the VNet from being moved while the Container App Environment
# exists.  Both must be deleted before Phase 3 can proceed.
# Environment deletion can take 10-15 minutes.
# =============================================================================

phase2_delete_old_app_and_env() {
  echo ">>> Phase 2: Deleting Container App '$APP_NAME' ..."
  az containerapp delete \
    --name "$APP_NAME" \
    --resource-group "$OLD_RG" \
    --yes

  echo ">>> Phase 2: Deleting Container App Environment '$ACE_NAME' (10-15 min) ..."
  az containerapp env delete \
    --name "$ACE_NAME" \
    --resource-group "$OLD_RG" \
    --yes

  echo ">>> Phase 2: Polling until environment is fully deleted ..."
  while az containerapp env show \
      --name "$ACE_NAME" \
      --resource-group "$OLD_RG" &>/dev/null 2>&1; do
    echo "    ... still deleting, checking again in 30s ..."
    sleep 30
  done
  echo ">>> Phase 2 complete — environment deleted."
}


# =============================================================================
# PHASE 3 — Move networking to new RG, preserving static outbound IP
#
# Azure does NOT support moving NAT Gateways between resource groups.
# The workaround that still preserves the static IP (52.187.226.64) is:
#   a) Detach NAT Gateway from subnet (idempotent — safe to re-run)
#   b) Delete the NAT Gateway — this releases the Public IP
#   c) Move the Public IP and VNet to new RG
#   d) Recreate the NAT Gateway in new RG, pointing at the moved Public IP
#   e) Attach the new NAT Gateway to the subnet in new RG
# =============================================================================

phase3_move_networking() {
  echo ">>> Phase 3a: Detaching NAT Gateway from subnet (if still attached) ..."
  CURRENT_NATGW=$(az network vnet subnet show \
    --name "$SUBNET_NAME" \
    --vnet-name "$VNET_NAME" \
    --resource-group "$OLD_RG" \
    --query "natGateway.id" -o tsv 2>/dev/null || echo "")

  if [ -n "$CURRENT_NATGW" ]; then
    az network vnet subnet update \
      --name "$SUBNET_NAME" \
      --vnet-name "$VNET_NAME" \
      --resource-group "$OLD_RG" \
      --remove natGateway
    echo "    NAT Gateway detached."
  else
    echo "    Already detached, skipping."
  fi

  echo ">>> Phase 3b: Deleting NAT Gateway in $OLD_RG (if still exists) ..."
  if az network nat gateway show --name "$NATGW_NAME" --resource-group "$OLD_RG" &>/dev/null 2>&1; then
    echo "    (NAT Gateways cannot be moved between RGs — deleting and recreating)"
    echo "    (The Public IP and its address are preserved)"
    az network nat gateway delete \
      --name "$NATGW_NAME" \
      --resource-group "$OLD_RG"
    echo "    NAT Gateway deleted."
  else
    echo "    Already deleted, skipping."
  fi

  echo ">>> Phase 3c: Moving Public IP and VNet to $NEW_RG (if not already there) ..."
  VNET_RG=$(az network vnet show --name "$VNET_NAME" --query resourceGroup -o tsv 2>/dev/null || echo "")
  if [ "$VNET_RG" != "$NEW_RG" ]; then
    PIP_ID=$(az network public-ip show \
      --name "$PIP_NAME" --resource-group "$OLD_RG" --query id -o tsv)
    VNET_ID=$(az network vnet show \
      --name "$VNET_NAME" --resource-group "$OLD_RG" --query id -o tsv)
    az resource move \
      --destination-group "$NEW_RG" \
      --ids "$PIP_ID" "$VNET_ID"
  else
    echo "    Already in $NEW_RG, skipping."
  fi

  echo ">>> Phase 3d: Recreating NAT Gateway in $NEW_RG with moved Public IP ..."
  PIP_ID=$(az network public-ip show \
    --name "$PIP_NAME" --resource-group "$NEW_RG" --query id -o tsv)
  az network nat gateway create \
    --name "$NATGW_NAME" \
    --resource-group "$NEW_RG" \
    --location "$LOCATION" \
    --public-ip-addresses "$PIP_ID" \
    --idle-timeout 10

  echo ">>> Phase 3e: Attaching NAT Gateway to subnet in $NEW_RG ..."
  NATGW_NEW_ID=$(az network nat gateway show \
    --name "$NATGW_NAME" --resource-group "$NEW_RG" --query id -o tsv)

  az network vnet subnet update \
    --name "$SUBNET_NAME" \
    --vnet-name "$VNET_NAME" \
    --resource-group "$NEW_RG" \
    --nat-gateway "$NATGW_NEW_ID"

  echo ">>> Phase 3 complete."
  echo "    Outbound IP preserved: $(az network public-ip show \
    --name "$PIP_NAME" --resource-group "$NEW_RG" --query ipAddress -o tsv)"
}


# =============================================================================
# PHASE 4 — Recreate Container App Environment in new RG
#
# This creates a NEW environment (required — Azure does not support moving
# Container App Environments between resource groups).  The environment
# gets a new random suffix in its URL, which is why the CNAME must change.
#
# At the end of this phase the script prints the NEW domain verification ID.
# You need this value to update your DNS TXT record before Phase 6.
# =============================================================================

phase4_create_environment() {
  echo ">>> Phase 4: Creating Container App Environment '$ACE_NAME' in $NEW_RG ..."
  echo "    (This takes 5-10 minutes)"

  INFRA_SUBNET_ID="/subscriptions/$SUBSCRIPTION/resourceGroups/$NEW_RG/providers/Microsoft.Network/virtualNetworks/$VNET_NAME/subnets/$SUBNET_NAME"

  az containerapp env create \
    --name "$ACE_NAME" \
    --resource-group "$NEW_RG" \
    --location "$LOCATION" \
    --infrastructure-subnet-resource-id "$INFRA_SUBNET_ID"

  NEW_VERIFICATION_ID=$(az containerapp env show \
    --name "$ACE_NAME" \
    --resource-group "$NEW_RG" \
    --query "properties.customDomainConfiguration.customDomainVerificationId" \
    -o tsv)

  echo ""
  echo ">>> Phase 4 complete."
  echo "    ┌─────────────────────────────────────────────────────────────────┐"
  echo "    │  NEW Domain Verification ID (update DNS TXT record — see below) │"
  echo "    │  $NEW_VERIFICATION_ID  │"
  echo "    └─────────────────────────────────────────────────────────────────┘"
  echo ""
}


# =============================================================================
# PHASE 5 — Recreate Container App in new RG
#
# The ACR admin password is fetched automatically.
# The two secret values at the top of this file must be filled in first.
# =============================================================================

phase5_create_container_app() {
  echo ">>> Phase 5: Fetching ACR admin password ..."
  ACR_PASSWORD=$(az acr credential show \
    --name "$ACR_NAME" \
    --query "passwords[0].value" -o tsv)

  echo ">>> Phase 5: Creating Container App '$APP_NAME' ..."
  az containerapp create \
    --name "$APP_NAME" \
    --resource-group "$NEW_RG" \
    --environment "$ACE_NAME" \
    --image "$IMAGE" \
    --registry-server "$ACR_SERVER" \
    --registry-username "$ACR_NAME" \
    --registry-password "$ACR_PASSWORD" \
    --target-port 8080 \
    --ingress external \
    --transport auto \
    --min-replicas 1 \
    --max-replicas 1 \
    --cpu 0.5 \
    --memory 1Gi \
    --secrets \
      "connstr=$CONNECTION_STRING" \
      "aad-client-secret=$AAD_CLIENT_SECRET" \
      "ctbdevacrazurecrio-ctbdevacr=$ACR_PASSWORD" \
    --env-vars \
      "ConnectionStrings__DefaultConnection=secretref:connstr" \
      "AzureAd__ClientSecret=secretref:aad-client-secret" \
      "ASPNETCORE_FORWARDEDHEADERS_ENABLED=true"

  NEW_FQDN=$(az containerapp show \
    --name "$APP_NAME" \
    --resource-group "$NEW_RG" \
    --query "properties.configuration.ingress.fqdn" -o tsv)

  echo ""
  echo ">>> Phase 5 complete."
  echo "    ┌─────────────────────────────────────────────────────────────────┐"
  echo "    │  NEW Container App FQDN (update DNS CNAME record — see below)   │"
  echo "    │  $NEW_FQDN            │"
  echo "    └─────────────────────────────────────────────────────────────────┘"
  echo ""
  echo "    Update your DNS records now (see DNS CHANGES at the bottom of this"
  echo "    file) and wait for propagation before running Phase 6."
}


# =============================================================================
# PHASE 6 — Bind custom domain with Azure-managed certificate
#
# MUST update DNS records first and wait for propagation.
# Both records must be live before this phase will succeed:
#   - TXT  asuid.supplier-poc.cookingthebooks.com.au → new verification ID
#   - CNAME supplier-poc.cookingthebooks.com.au      → new Container App FQDN
#
# Azure will issue a free managed TLS certificate automatically.
# =============================================================================

phase6_bind_custom_domain() {
  echo ">>> Phase 6: Adding custom domain hostname ..."
  az containerapp hostname add \
    --hostname "$CUSTOM_DOMAIN" \
    --resource-group "$NEW_RG" \
    --name "$APP_NAME"

  echo ">>> Phase 6: Binding managed TLS certificate ..."
  az containerapp hostname bind \
    --hostname "$CUSTOM_DOMAIN" \
    --resource-group "$NEW_RG" \
    --name "$APP_NAME" \
    --environment "$ACE_NAME" \
    --validation-method CNAME

  echo ""
  echo ">>> Phase 6 complete."
  echo "    Site should be live at https://$CUSTOM_DOMAIN"
  echo "    Verify it before running Phase 7."
}


# =============================================================================
# PHASE 7 — Clean up old resources (run after verifying the site works)
#
# By this point the VNet, NAT GW, and Public IP have already moved to the
# new RG.  The old RG (ctb-dev-rg) will only contain the ACR (ctbdevacr).
#
# Options:
#   a) Move the ACR to ctb-dev-poc-rg as well (uncomment ACR_MOVE below).
#   b) Leave the ACR in ctb-dev-rg (it can serve images across RGs).
#   c) Delete ctb-dev-rg entirely once confirmed empty.
# =============================================================================

phase7_cleanup() {
  echo ">>> Phase 7: Checking what remains in $OLD_RG ..."
  az resource list --resource-group "$OLD_RG" --output table

  # Option a: Move ACR to new RG
  # ACR_ID=$(az acr show --name "$ACR_NAME" --resource-group "$OLD_RG" --query id -o tsv)
  # az resource move --destination-group "$NEW_RG" --ids "$ACR_ID"

  # Option c: Delete old RG (only once confirmed empty or ACR moved)
  # az group delete --name "$OLD_RG" --yes

  echo ">>> Phase 7 complete."
}


# =============================================================================
# MAIN — comment out phases you have already run or want to skip
# =============================================================================

# phase1_create_resource_group
# phase2_delete_old_app_and_env
# phase3_move_networking  -- DONE (VNet + PIP moved; NAT GW created via PowerShell)
phase4_create_environment
phase5_create_container_app

echo "==================================================================="
echo "  STOP — Update DNS records now (see bottom of this file),"
echo "  wait for propagation, then run Phase 6."
echo "==================================================================="
# phase6_bind_custom_domain   # <-- uncomment and re-run once DNS is live
# phase7_cleanup              # <-- uncomment and re-run after site verified


# =============================================================================
# DNS CHANGES REQUIRED
#
# You will have two values from Phase 4 and Phase 5 output.
# Both records must be updated BEFORE running Phase 6.
#
# 1. TXT record — domain ownership verification for the new environment:
#
#      Name:   asuid.supplier-poc.cookingthebooks.com.au
#              (some DNS providers use just: asuid.supplier-poc)
#      Type:   TXT
#      Value:  <NEW verification ID printed by Phase 4>
#      TTL:    300 (or lowest available)
#
#      Old value (now invalid): 581B7DB1F9207A527A25333C507BBDA4C8BAD65D364616A29DE6D5D2BB5C6C56
#
# 2. CNAME record — routes traffic to the new Container App:
#
#      Name:   supplier-poc.cookingthebooks.com.au
#              (some DNS providers use just: supplier-poc)
#      Type:   CNAME
#      Value:  <NEW Container App FQDN printed by Phase 5>
#              (format: ctb-supplier.<new-random-id>.australiaeast.azurecontainerapps.io)
#      TTL:    300 (or lowest available)
#
#      Old value (will stop working): ctb-supplier.livelysky-e2cd8543.australiaeast.azurecontainerapps.io
#
# NOTE: The static outbound IP (52.187.226.64) is PRESERVED — no changes
# needed for any firewall rules or allowlists that reference this IP.
# =============================================================================
