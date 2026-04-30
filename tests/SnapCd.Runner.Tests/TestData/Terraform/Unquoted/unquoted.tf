variable subscription_id {}

variable tenant_id {}

variable name {}

variable location {}

variable fqdn {}

variable "enable_custom_dns" {
  type    = bool
  default = true
}

variable "proxied" {
  type    = bool
  default = true
}

variable resource_group_name {}

variable backend_akv_id {}

variable cloudflare_zone_id {}
