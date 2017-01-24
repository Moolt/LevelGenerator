public enum GizmoPreviewState { HIDDEN, ONSELECTION, ALWAYS }
//Used by AbstractShape
public enum MeshShape{ CUBE, RAMP, TRIANGULAR, CYLINDER, PLANE }
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