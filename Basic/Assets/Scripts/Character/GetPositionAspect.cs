using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public readonly partial struct GetPositionAspect : IAspect
{

    public readonly RefRO<LocalTransform> Transform;
}
