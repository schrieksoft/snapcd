terraform {
  required_providers {
    random = {
      source  = "hashicorp/random"
      version = "3.9.1"
    }
  }
}

variable "from_a" {
  type = string
}

resource "random_pet" "b" {
  prefix = var.from_a
}

output "b_id" {
  value = random_pet.b.id
}

# Not derived from var.from_a, so it is unaffected when a's value becomes unknown. f reads this.
resource "random_pet" "b_stable" {
  keepers = {
    fixed = "constant"
  }
}

output "b_other" {
  value = random_pet.b_stable.id
}
