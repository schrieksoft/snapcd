terraform {
  required_providers {
    random = {
      source  = "hashicorp/random"
      version = "3.9.1"
    }
  }
}

# A second origin, independent of a. Its keepers force replacement the same way, so g_id goes
# unknown and b, which reads both origins, is owed a probe by two waves at once.
resource "random_pet" "g" {
  keepers = {
    generation = "2"
  }
}

output "g_id" {
  value = random_pet.g.id
}
