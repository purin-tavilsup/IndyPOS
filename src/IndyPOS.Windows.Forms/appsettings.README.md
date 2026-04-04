# WinForms Configuration Guide

This document explains each configuration property in `appsettings.json`.

---

## Report

### `Directory`

**What:** Local directory where generated reports are saved.

**How to get:**
- Use default `C:\ProgramData\IndyPOS\Reports`
- Or specify any writable directory

**Used by:** Report generation features (daily sales reports, etc.)

**Example:**
```json
"Report": {
  "Directory": "C:\\ProgramData\\IndyPOS\\Reports"
}
```

---

## Store

### `ConfigPath`

**What:** Path to the store configuration JSON file.

**How to get:**
- Use default `C:\ProgramData\IndyPOS\Config\StoreConfiguration.json`
- Created by `install-config.ps1` or manually

**Used by:**
- Receipt printing (store name, address, phone)
- Barcode scanner device selection
- Printer selection

**Store Configuration File Contents:**
```json
{
  "StoreFullName": "My Store Name",
  "StoreName": "My Store",
  "StoreAddressLine1": "123 Main Street",
  "StoreAddressLine2": "City 12345",
  "StorePhoneNumber": "000-000-0000",
  "PrinterName": "XP-58",
  "BarcodeScannerDeviceName": "",
  "SerialPortName": "COM1",
  "Code": 1
}
```

**Example:**
```json
"Store": {
  "ConfigPath": "C:\\ProgramData\\IndyPOS\\Config\\StoreConfiguration.json"
}
```

---

## StoreHub

Settings for connecting to the StoreHub local API service.

### `BaseUrl`

**What:** URL where StoreHub API is running.

**How to get:**
- Local machine: `http://localhost:5000`
- Different machine on LAN: `http://<STOREHUB_IP>:5000`

**Used by:** All API calls (login, products, sales, etc.)

### `TimeoutSeconds`

**What:** HTTP request timeout in seconds.

**How to get:**
- Default: `30` seconds
- Increase if experiencing timeout errors on slow networks

**Used by:** HttpClient for all StoreHub API requests.

### `AutoSyncProductsOnStartup`

**What:** Whether to fetch all products from StoreHub when the app starts.

**How to get:**
- Default: `true` (recommended)
- Set to `false` if you want faster startup (products loaded on-demand)

**Used by:** Product cache initialization on app startup.

**Example:**
```json
"StoreHub": {
  "BaseUrl": "http://localhost:5000",
  "TimeoutSeconds": 30,
  "AutoSyncProductsOnStartup": true
}
```

---

## Complete Example

```json
{
  "Report": {
    "Directory": "C:\\ProgramData\\IndyPOS\\Reports"
  },
  "Store": {
    "ConfigPath": "C:\\ProgramData\\IndyPOS\\Config\\StoreConfiguration.json"
  },
  "StoreHub": {
    "BaseUrl": "http://localhost:5000",
    "TimeoutSeconds": 30,
    "AutoSyncProductsOnStartup": true
  }
}
```

---

## Multi-Terminal Setup

For POS terminals connecting to a central StoreHub on another machine:

```json
{
  "Report": {
    "Directory": "C:\\ProgramData\\IndyPOS\\Reports"
  },
  "Store": {
    "ConfigPath": "C:\\ProgramData\\IndyPOS\\Config\\StoreConfiguration.json"
  },
  "StoreHub": {
    "BaseUrl": "http://192.168.1.100:5000",
    "TimeoutSeconds": 30,
    "AutoSyncProductsOnStartup": true
  }
}
```

**Note:** Replace `192.168.1.100` with the actual IP address of the StoreHub machine.

---

## Troubleshooting

### "Connection refused" error
- Verify StoreHub service is running: `Get-Service IndyPOS.StoreHub`
- Check firewall allows port 5000
- Verify `BaseUrl` is correct

### "Timeout" errors
- Increase `TimeoutSeconds` value
- Check network connectivity to StoreHub
- Verify StoreHub health: `curl http://<BaseUrl>/health`

### Products not loading
- Set `AutoSyncProductsOnStartup` to `true`
- Check StoreHub logs for errors
- Verify database connection is working
