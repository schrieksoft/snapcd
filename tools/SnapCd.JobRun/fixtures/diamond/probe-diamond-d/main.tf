terraform {
  required_providers {
    random = {
      source  = "hashicorp/random"
      version = "3.9.1"
    }
  }
}

variable "from_b" {
  type = string
}

variable "from_c" {
  type = string
}

# Reads both arms, so d cannot be planned until b and c have both reported.
resource "random_pet" "d" {
  prefix = "${var.from_b}-${var.from_c}"
}

output "d_id" {
  value = random_pet.d.id
}
