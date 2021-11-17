# pnp basic 

Reference PnP device implementation

## Model

## Twin States

New Device, initial version

```json
{
	"desired": {
			"$metadata": {
				"$lastUpdated": "2021-11-17T07:28:24.8546444Z"
			},
			"$version": 1
		},
		"reported": {
			"enabled": {
				"ad": "init with default values from device",
				"av": 1,
				"ac": 203,
				"value": true
			},
			"interval": {
				"ad": "init with default device value",
				"av": 1,
				"ac": 203,
				"value": 6
			},
			"started": "2021-11-16T23:29:47.1907739-08:00"
		}
}
```

Service updates interval

```json
	"desired": {
			"interval": 3,
			"$metadata": {
				"$lastUpdated": "2021-11-17T07:33:53.3497592Z",
				"$lastUpdatedVersion": 2,
				"interval": {
					"$lastUpdated": "2021-11-17T07:33:53.3497592Z",
					"$lastUpdatedVersion": 2
				}
			},
			"$version": 2
		},
		"reported": {
			"enabled": {
				"ad": "init with default values from device",
				"av": 1,
				"ac": 203,
				"value": true
			},
			"interval": {
				"ad": "desired notification accepted",
				"av": 2,
				"ac": 200,
				"value": 3
			},
			"started": "2021-11-16T23:33:11.7776861-08:00",
		}
	}
}
```
