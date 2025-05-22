enum PlacementObjectType {
  None,
  SafeObject,
  DamageObject,
}


interface IPlacementObject
{
  PlacementObjectType ObjType { get; }
  Vector3 GetPosition();
  void Action();
}
