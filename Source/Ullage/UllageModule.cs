﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RealFuels.Ullage
{
    public class UllageModule : VesselModule
    {
        List<UllageSet> ullageSets;
        List<Tanks.ModuleFuelTanks> tanks;

        Vessel vessel;

        int partCount = -1;

        public void Start()
        {
            vessel = GetComponent<Vessel>();
            ullageSets = new List<UllageSet>();
            Reset();
        }

        public void FixedUpdate()
        {
            if (vessel == null)
                return;
            Vector3 accel;
            Vector3 angVel;
            if (TimeWarp.WarpMode == TimeWarp.Modes.HIGH && TimeWarp.CurrentRate > TimeWarp.MaxPhysicsRate)
            {
                // Time warping... (5x -> 100000x)
                angVel = Vector3.zero; // FIXME support rotation in timewarp!
                if (vessel.LandedOrSplashed)
                    accel = -(Vector3)FlightGlobals.getGeeForceAtPosition(vessel.GetWorldPos3D());
                else
                    accel = Vector3.zero;
            }
            else
            {
                accel = (Vector3)(vessel.acceleration - FlightGlobals.getGeeForceAtPosition(vessel.GetWorldPos3D()));
                angVel = vessel.angularVelocity;
            }

            // get boiloff accel
            double massRate = 0d;
            double ventingAcceleration = 0d;
            for (int i = tanks.Count - 1; i >= 0; --i)
                massRate += tanks[i].BoiloffMassRate;

            // technically we should vent in the correct direction per engine's tanks
            // Instead, this will just give magical "correct direction" acceleration from total
            // boiloff mass, for every engine (i.e. for every orientation)
            if (massRate > 0d)
            {
                double vesselMass = 0d;
                for (int i = vessel.Parts.Count - 1; i >= 0; --i)
                {
                    Part p = vessel.Parts[i];
                    if (p.rb != null)
                        vesselMass += p.rb.mass;
                }
                ventingAcceleration = massRate / vesselMass * RFSettings.Instance.ventingVelocity;
            }

            // Update ullage sims
            for (int i = ullageSets.Count - 1; i >= 0; --i)
            {
                ullageSets[i].Update(accel, angVel, TimeWarp.fixedDeltaTime, ventingAcceleration);
            }
        }

        public void Reset()
        {
            ullageSets.Clear();

            for (int i = vessel.Parts.Count - 1; i >= 0; --i)
            {
                Part part = vessel.Parts[i];
                for (int j = part.Modules.Count - 1; j >= 0; --j)
                {
                    PartModule m = part.Modules[j];
                    if (m is Tanks.ModuleFuelTanks)
                    {
                        Tanks.ModuleFuelTanks tank = m as Tanks.ModuleFuelTanks;
                        if (!tanks.Contains(tank))
                            tanks.Add(tank);
                    }
                    else if (m is ModuleEnginesRF)
                    {
                        ModuleEnginesRF engine = m as ModuleEnginesRF;
                        
                        if (engine.ullageSet == null) // just in case
                            engine.ullageSet = new UllageSet(engine);

                        ullageSets.Add(engine.ullageSet);
                    }
                }
            }
        }
    }
}