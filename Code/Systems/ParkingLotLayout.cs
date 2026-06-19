public sealed class ParkingLotLayout : Component
{
	public static ParkingLotLayout Instance { get; private set; }

	[Property] public float MainRoadX { get; set; } = 0f;
	[Property] public float ConnectorY { get; set; } = 0f;
	[Property] public float EntryBarrierY { get; set; } = 350f;
	[Property] public float ExitBarrierY { get; set; } = -350f;
	[Property] public float VehicleHalfLength { get; set; } = 96.5f;
	[Property] public float BarrierClearance { get; set; } = 28f;
	[Property] public float SpawnLeadDistance { get; set; } = 80f;
	[Property] public float SpawnQueueSpacing { get; set; } = 90f;
	[Property] public float RowNorthY { get; set; } = 150f;
	[Property] public float RowMiddleY { get; set; } = 0f;
	[Property] public float RowSouthY { get; set; } = -150f;
	[Property] public float SlotSpacing { get; set; } = 100f;
	[Property] public int SlotsPerRow { get; set; } = 4;

	protected override void OnStart()
	{
		if ( Instance != null && Instance != this )
		{
			Enabled = false;
			return;
		}

		Instance = this;
	}

	protected override void OnDestroy()
	{
		if ( Instance == this )
		{
			Instance = null;
		}
	}

	public float GetSlotX( int slotIndex )
	{
		return (slotIndex - (SlotsPerRow - 1) * 0.5f) * SlotSpacing;
	}

	public float GetRowY( int rowIndex )
	{
		return rowIndex switch
		{
			0 => RowNorthY,
			1 => RowMiddleY,
			_ => RowSouthY
		};
	}

	public Vector3 GetEntryWaitPosition()
	{
		return new Vector3( MainRoadX, EntryBarrierY + VehicleHalfLength + BarrierClearance, 0f );
	}

	public Vector3 GetExitWaitPosition()
	{
		return new Vector3( MainRoadX, ExitBarrierY - VehicleHalfLength - BarrierClearance, 0f );
	}

	public Vector3 GetSpawnPosition( int queueIndex )
	{
		var entryWaitY = GetEntryWaitPosition().y;
		return new Vector3( MainRoadX, entryWaitY + SpawnLeadDistance + queueIndex * SpawnQueueSpacing, 0f );
	}

	public Rotation GetSpawnRotation()
	{
		return Rotation.LookAt( new Vector3( 0, -1, 0 ), Vector3.Up );
	}

	public Vector3 GetEntryQueuePosition( int queueIndex )
	{
		var entryWait = GetEntryWaitPosition();
		return new Vector3( MainRoadX, entryWait.y + queueIndex * SpawnQueueSpacing, 0f );
	}

	public Vector3 GetParkingPosition( ParkingSpot spot )
	{
		return new Vector3( spot.SlotX, spot.RowY, 0f );
	}

	public List<Vector3> BuildPathToSpot( ParkingSpot spot, int entryQueueIndex )
	{
		var slotX = spot.SlotX;
		return new List<Vector3>
		{
			GetEntryQueuePosition( entryQueueIndex ),
			new Vector3( MainRoadX, ConnectorY, 0f ),
			new Vector3( slotX, ConnectorY, 0f ),
			GetParkingPosition( spot )
		};
	}

	public List<Vector3> BuildPathToExit( Vector3 fromPosition )
	{
		return new List<Vector3>
		{
			new Vector3( fromPosition.x, ConnectorY, 0f ),
			new Vector3( MainRoadX, ConnectorY, 0f ),
			GetExitWaitPosition()
		};
	}
}
