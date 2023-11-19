package main

import (
	"testing"
	"time"
)

func Test_TryParseHumanReadableDateTimeOffset(t *testing.T) {
	now := time.Now()
	expected := time.Date(now.Year(), now.Month(), now.Day()-1, 0, 0, 0, 0, now.Location())
	actual, _ := TryParseHumanReadableDateTimeOffset("1 day")

	if actual != expected {
		t.Errorf("TryParseHumanReadableDateTimeOffset() = %v; want %v", actual, expected)
	}
}
