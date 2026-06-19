import json
import uuid
import copy
from pathlib import Path

path = Path(__file__).resolve().parents[1] / "Assets" / "scenes" / "parking_test.scene"
data = json.loads(path.read_text(encoding="utf-8"))

for go in data["GameObjects"]:
    if go["Name"] == "Player Controller":
        go["Position"] = "0,-40,0"
    if go["Name"] == "Vehicle Spawn Point":
        go["Position"] = "0,555,0"
    if go["Name"] == "Systems":
        go["Components"].append({
            "__type": "ParkingLotLayout",
            "__guid": str(uuid.uuid4()),
            "__enabled": True,
            "Flags": 0,
            "ConnectorY": 0,
            "EntryBarrierY": 350,
            "ExitBarrierY": -350,
            "VehicleHalfLength": 96.5,
            "BarrierClearance": 28,
            "SpawnLeadDistance": 80,
            "SpawnQueueSpacing": 90,
            "MainRoadX": 0,
            "OnComponentDestroy": None,
            "OnComponentDisabled": None,
            "OnComponentEnabled": None,
            "OnComponentFixedUpdate": None,
            "OnComponentStart": None,
            "OnComponentUpdate": None,
            "RowNorthY": 100,
            "RowSouthY": -100,
            "SlotSpacing": 100,
        })
        go["Components"].append({
            "__type": "TrafficSystem",
            "__guid": str(uuid.uuid4()),
            "__enabled": True,
            "Flags": 0,
            "OnComponentDestroy": None,
            "OnComponentDisabled": None,
            "OnComponentEnabled": None,
            "OnComponentFixedUpdate": None,
            "OnComponentStart": None,
            "OnComponentUpdate": None,
            "SafeDistance": 150,
            "MaxEntryQueue": 1,
        })
    if go["Name"] == "Barriers":
        for child in go.get("Children", []):
            if child["Name"] == "Entry Barrier":
                child["Position"] = "0,350,0"
            if child["Name"] == "Exit Barrier":
                child["Position"] = "0,-350,0"
    if go["Name"] == "Parking Spots":
        slots = []
        for row, y in [(0, 100), (1, -100)]:
            for i in range(5):
                x = (i - 2) * 100
                slots.append((row, i, x, y))
        children = []
        template = go["Children"][0]
        for idx, (row, slot, x, y) in enumerate(slots, 1):
            spot = copy.deepcopy(template)
            spot["__guid"] = str(uuid.uuid4())
            spot["Name"] = f"Parking Spot {idx}"
            spot["Position"] = f"{x},{y},2"
            spot["Scale"] = "1,1.4,1"
            for comp in spot["Components"]:
                comp["__guid"] = str(uuid.uuid4())
                if comp["__type"] == "ParkingSpot":
                    comp["RowIndex"] = row
                    comp["SlotIndex"] = slot
            children.append(spot)
        go["Children"] = children

path.write_text(json.dumps(data, indent=2), encoding="utf-8")
print("parking_test.scene updated")
