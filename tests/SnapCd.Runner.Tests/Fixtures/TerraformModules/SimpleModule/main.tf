# Simple test module for SnapCD Runner tests
# Uses basic resources that don't require external services

resource "null_resource" "test_resource" {
  triggers = {
    message = var.test_message
    count   = var.resource_count
  }
}

resource "random_uuid" "test_id" {
  keepers = {
    message = var.test_message
  }
}
