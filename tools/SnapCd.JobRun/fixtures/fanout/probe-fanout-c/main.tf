terraform {
  required_providers {
    null = {
      source  = "hashicorp/null"
      version = "3.2.4"
    }
  }
}

variable "from_a" {
  type = string
}

# Nothing reads from_a, so c is genuinely unaffected by a's change. Its three outputs are still
# reported by a probe, because a consumer needs a value for every variable it reads, and all three
# come back as no-ops: the case a panel counting signal rows calls "changes".
resource "null_resource" "c_stable" {
  triggers = {
    fixed = "constant"
  }
}

output "c_ip" {
  value = "100.0.8.255"
}

output "c_fqdn" {
  value = "fanout-c.example.com"
}

output "c_id" {
  value = "settled-marmot-sunny-crab"
}
