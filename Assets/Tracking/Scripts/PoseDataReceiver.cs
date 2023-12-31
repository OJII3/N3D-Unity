using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniJSON;

namespace Tracking
{
    public class PoseDataReceiver
    {
        public PoseLandmark[] landmarks { get; private set; }
    }
}