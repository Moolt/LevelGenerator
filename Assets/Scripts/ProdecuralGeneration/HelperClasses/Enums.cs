public enum GizmoPreviewState { HIDDEN, ONSELECTION, ALWAYS }
//Used by AbstractShape
public enum MeshShape{ CUBE, RAMP, TRIANGULAR, CYLINDER, PLANE, TERRAIN }
//Used by AbstractProperty
public enum PropertyType { INSTANTIATING, TRANSFORMING, MESHGENERATION };
//Used by AbstractScaling
public enum Interval { MIN, MAX };
//Used by LinearArray
public enum Direction { XAXIS, YAXIS, ZAXIS }
//Used by ObjectDocking
public enum OffsetType { ABSOLUTE, RELATIVE, RELATIVEUNIFORM }
//Used by ScatteredArray
public enum AreaType{ SPHERICAL, RECT, ABSTRACTBOUNDS }
//Used by ChunkInstantiator
public enum ProcessType{ GENERATE, INEDITOR, PREVIEW }
//Used by AbstractProperty
public enum RemovalTime{ INSTANTLY, DELAYED, MANUAL }
//public enum SquareDirection { HORIZONTAL, VERTICAL }