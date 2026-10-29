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

# Updated in place when a's value moves, and an in-place update keeps the resource's id. One at
# the root and one in a child module, so a reader is told where each change lives.
resource "null_resource" "b_first" {
  triggers = {
    upstream = var.from_a
  }
}

module "inner" {
  source  = "./inner"
  from_a  = var.from_a
}

# Published from neither, so b passes nothing on while changing: the case a panel counting only
# output signals calls unaffected.
resource "null_resource" "b_stable" {
  triggers = {
    fixed = "constant"
  }
}

output "b_id" {
  value = "settled-marmot-plain-asp"
}
