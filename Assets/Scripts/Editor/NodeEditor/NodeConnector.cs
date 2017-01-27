using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum ConnectorType{ STRING, FLOAT, INT, REFERENCE }

public class NodeConnector {
	public BaseNode reference;
	public Type nodeType;
	public int intValue;
	public float floatValue;
	public string stringValue;
	public GameObject referenceValue;
}
