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

# b reads a second origin as well, so two waves can be open on it at once, each predicting a
# different value for one of these. Each wave is owed its own probe.
variable "from_g" {
  type = string
}

resource "random_pet" "b" {
  prefix = "${var.from_a}-${var.from_g}"
}

output "b_id" {
  value = random_pet.b.id
}

# Derived from neither variable, so it is unaffected when an upstream's value becomes unknown. f reads this.
resource "random_pet" "b_stable" {
  keepers = {
    fixed = "constant"
  }
}

output "b_other" {
  value = random_pet.b_stable.id
}
