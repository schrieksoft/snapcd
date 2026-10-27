package signals

// actionString renders a plan action the way the jsonplan package writes it, so a
// document carries the same wire form as `tofu show -json`. jsonplan keeps its own
// copy unexported; its inverse, jsonplan.UnmarshalActions, is public and reverses
// this.
func actionString(action string) []string {
	switch action {
	case "NoOp":
		return []string{"no-op"}
	case "Create":
		return []string{"create"}
	case "Delete":
		return []string{"delete"}
	case "Update":
		return []string{"update"}
	case "CreateThenDelete":
		return []string{"create", "delete"}
	case "ForgetThenCreate":
		return []string{"forget", "create"}
	case "Read":
		return []string{"read"}
	case "DeleteThenCreate":
		return []string{"delete", "create"}
	case "Forget":
		return []string{"forget"}
	default:
		return []string{action}
	}
}
