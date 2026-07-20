# Requirements: SimHub Shift Light Integration for Busylight

This document outlines the requirements and design decisions for controlling a physical Busylight device as a racing simulator shift light using telemetry data from SimHub.

---

## 1. Objective
Build a Python-based background service/script that connects to a local SimHub instance, reads real-time engine telemetry (specifically RPM/redline data), and maps it to a physical Busylight device to act as a shift light indicator (switching between off, yellow, and red).

---

## 2. Telemetry Retrieval (SimHub Interface)
* **Integration Method**: Retrieve telemetry data from the local SimHub web server.
* **Connection**: Locally hosted on the same PC (`http://127.0.0.1:8888`).
* **API Endpoint**: `http://127.0.0.1:8888/api/getgamedata` (JSON polling).
* **Update Frequency**: Every 30 ms (approx. 33 Hz polling rate).
* **Telemetry Properties to Monitor**:
  * `NewData.Rpms`: Current engine RPM.
  * `NewData.CarSettings_ShiftLightRPM`: Primary shift point RPM.
  * `NewData.CarSettings_MaxRPM`: Maximum vehicle RPM.
  * `NewData.Gear`: Current gear.
  * `NewData.PitLimiterOn`: Pit lane speed limiter status.

---

## 3. Light States & Shift Light Behavior
We need to map RPM levels to light colors. The following states are configured based on the vehicle's shift points as defined by SimHub:

| State | Condition | Busylight Color |
|---|---|---|
| **Low RPM** | RPM < Yellow Threshold | Off |
| **Mid RPM** | Yellow Threshold <= RPM < Shift RPM | Yellow (Solid) |
| **Shift Point / Redline** | RPM >= Shift RPM | Red (Flashing Rapidly) |
| **Special Gears** | Gear is Neutral (`N`), Reverse (`R`), or Pit Limiter is active | Off |

### 3.1. Threshold Calculations
* **Shift RPM (Target)**: If `NewData.CarSettings_ShiftLightRPM` is not provided (null, 0, or missing), calculate it as **95%** of `NewData.CarSettings_MaxRPM`.
* **Yellow Threshold**: Calculated as **85%** of the Shift RPM.

---

## 4. Engineering Implementation Decisions

### 4.1. Hardware Integration
The script uses `busylight-core` to communicate with the physical Busylight device. 

### 4.2. Latency & Polling
Polling will run in a fast loop with a sleep interval defined in the configuration (defaulting to 30ms).

### 4.3. Flashing Behavior
When the RPM meets the redline threshold, the indicator will flash by toggling the Busylight between red and off. The default toggle interval is **50 ms** (10Hz flash frequency).

### 4.4. Configuration File (`config.json`)
The script will load settings from a local `config.json` file. If the file is missing, it should automatically fall back to sensible defaults. Configurable options include:
* SimHub base URL / port.
* Polling interval (ms).
* Fallback shift RPM percentage (default `0.95`).
* Yellow threshold percentage (default `0.85`).
* Red flashing toggle interval (ms, default `50`).
* RGB values for Yellow and Red states.
