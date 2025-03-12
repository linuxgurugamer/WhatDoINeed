using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatDoINeed
{
    // This avoids the need to have SCANsat as a reference.
    // Also avoids the confusion of the blended entries which aren't needed
    // to identify the scanner type on a part
    public enum SCANsatSCANtype : short
    {
        Nothing = 0,
        AltimetryLoRes = 1,
        AltimetryHiRes = 2,
        //Altimetry = 3,
        VisualLoRes = 4,
        Biome = 8,
        Anomaly = 16,
        AnomalyDetail = 32,
        VisualHiRes = 64,
        ResourceLoRes = 128,
        Resources = 256,  //While SCANsat defines 256 as ResourceHiRes, there isn't a ResourceHiRes experiment defined, only Resources
        //ResourceHiRes = 256,

        //Everything_SCAN = 511,
        //Science = 143,
        //Everything = short.MaxValue
    }
}
