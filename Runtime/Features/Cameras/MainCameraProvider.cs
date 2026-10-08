using UnityEngine;

namespace _Project.Features.Cameras
{
    public class MainCameraProvider : IMainCameraProvider
    {
        private Camera _camera;

        public Camera Camera
        {
            get
            {
                if (_camera == null)
                {
                    _camera = Camera.main;
                }

                return _camera;
            }
        }
    }
}
