using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BveTypes.ClassWrappers;

namespace Automatic9045.BveEx.CityOneman
{
    internal class StationListEx
    {
        private readonly MapObjectList Stations;
        private readonly Func<double> LocationGetter;

        public StationListEx(MapObjectList stations, Func<double> locationGetter)
        {
            Stations = stations;
            LocationGetter = locationGetter;
        }

        public (int Index, Station Station) GetStation(int indexDelta)
        {
            int index = Stations.CurrentIndex + indexDelta;
            return index < 0 || Stations.Count <= index ? (-1, null) : (index, Stations[index] as Station);
        }

        public bool IsNearestStation(int index)
        {
            Station station = Stations[index] as Station;

            Station previousStation = index == 0 ? null : Stations[index - 1] as Station;
            Station nextStation = Stations.Count <= index + 1 ? null : Stations[index + 1] as Station;

            double min = previousStation is null ? double.MinValue : station.MinStopPosition - (station.MinStopPosition - previousStation.MaxStopPosition) / 2;
            double max = nextStation is null ? double.MaxValue : station.MaxStopPosition + (nextStation.MinStopPosition - station.MaxStopPosition) / 2;

            double location = LocationGetter();

            return min <= location && location <= max;
        }

        public bool IsAtValidPosition(int index)
        {
            Station station = Stations[index] as Station;
            double location = LocationGetter();

            return station.MinStopPosition <= location && location <= station.MaxStopPosition;
        }
    }
}
