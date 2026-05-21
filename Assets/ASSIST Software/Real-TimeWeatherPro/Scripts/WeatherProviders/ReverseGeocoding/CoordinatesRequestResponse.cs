//
// Copyright(c) 2023 Real-Time Weather ASSIST Software
// https://assist-software.net
//
// For information about the licensing and copyright of this software please
// contact ASSIST Software at 3d_support@assist.ro
//

using UnityEngine;

namespace RealTimeWeather.Data
{
    public class CoordinatesResponseData
    {
        public int place_id;
        public string licence;
        public string osm_type;
        public int osm_id;
        public Vector4 bbox;
        public string lat;
        public string lon;
        public string display_name;
        public string category;
        public string type;
        public int importance;
        public string icon;
    }
}