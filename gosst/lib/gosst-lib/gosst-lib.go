package main

import (
	"C"
	"encoding/json"
	"unsafe"

	"github.com/ugorji/go/codec"

	psst "gosst/formats/psst"
	sst "gosst/formats/sst"
)

type calibrations struct {
	FrontCalibration psst.Calibration `json:"front"`
	RearCalibration  psst.Calibration `json:"rear"`
}

//export GeneratePsst
func GeneratePsst(data unsafe.Pointer, dataSize C.int, lnk unsafe.Pointer, lnkSize C.int, calibrations unsafe.Pointer, calibrationsSize C.int) (unsafe.Pointer, int) {
	sstBytes := C.GoBytes(data, dataSize)
	lnkBytes := C.GoBytes(lnk, lnkSize)
	calibrationsBytes := C.GoBytes(calibrations, calibrationsSize)

	var linkage psst.Linkage
	if err := json.Unmarshal(lnkBytes, &linkage); err != nil {
		return nil, -1
	}
	if err := linkage.ProcessRawData(); err != nil {
		return nil, -2
	}

	fcal, rcal, err := psst.LoadCalibrations(calibrationsBytes, linkage)
	if err != nil {
		return nil, -3
	}

	front, rear, meta, err := sst.ProcessRaw(sstBytes)
	if err != nil {
		return nil, -4
	}
	setup := psst.SetupData{
		Linkage:          &linkage,
		FrontCalibration: fcal,
		RearCalibration:  rcal,
	}
	pd, err := psst.ProcessRecording(front, rear, meta, &setup)
	if err != nil {
		return nil, -5
	}

	var psst []byte
	var h codec.MsgpackHandle
	enc := codec.NewEncoderBytes(&psst, &h)
	enc.Encode(pd)

	return C.CBytes(psst), len(psst)
}

func main() {}

// compile the code as:
// go build -ldflags="-s -w" -buildmode=c-shared -o gosst.dll main.go
