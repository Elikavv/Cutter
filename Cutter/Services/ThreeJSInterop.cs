using Microsoft.JSInterop;
using System.ComponentModel;

namespace Cutter.Services
{
    public class ThreeJSInterop : IAsyncDisposable
    {
        private readonly IJSRuntime _jsRuntime;
        private IJSObjectReference? _threeJSModule;
        private bool _isInitialized = false;
        private string? _currentContainerId;

        public ThreeJSInterop(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task Initialize(string containerId)
        {
            if (_isInitialized && _currentContainerId == containerId)
                return;

            try
            {
                // Проверяем, что Three.js загружен
                await _jsRuntime.InvokeVoidAsync("eval",
                    "if (typeof THREE === 'undefined') throw new Error('Three.js not loaded');");

                await _jsRuntime.InvokeVoidAsync("ThreeJSInterop.initialize", containerId);

                // Загружаем модуль с функциями Three.js
                /*_threeJSModule = await _jsRuntime.InvokeAsync<IJSObjectReference>(
                    "import", "./js/threeJsInterop.js");

                await _threeJSModule.InvokeVoidAsync("initialize", containerId);*/

                _currentContainerId = containerId;
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ThreeJS initialization failed: {ex.Message}");
                throw;
            }
        }



        public async Task RenderSheet(double sheetWidth, double sheetHeight, object[] details)
        {


            /*if (!_isInitialized || _threeJSModule == null)
                throw new InvalidOperationException("ThreeJS not initialized");*/

            try
            {
                /*await _threeJSModule.InvokeVoidAsync(
                    "renderSheet",
                    _currentContainerId,
                    sheetWidth,
                    sheetHeight,
                    details);*/

                await _jsRuntime.InvokeVoidAsync("ThreeJSInterop.renderSheet",
                    _currentContainerId, sheetWidth, sheetHeight, details);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ThreeJS render failed: {ex.Message}");
                throw;
            }
        }

        public async Task SetZoom(double zoomLevel)
        {
            /*if (!_isInitialized || _threeJSModule == null)
                return;*/

            try
            {
                /*await _threeJSModule.InvokeVoidAsync(
                    "setZoom",
                    _currentContainerId,
                    zoomLevel);*/

                await _jsRuntime.InvokeVoidAsync("ThreeJSInterop.setZoom", _currentContainerId, zoomLevel);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ThreeJS zoom failed: {ex.Message}");
            }
        }

        public async Task ResetCamera()
        {
            /*if (!_isInitialized || _threeJSModule == null)
                return;*/

            try
            {
                /*await _threeJSModule.InvokeVoidAsync(
                    "resetCamera",
                    _currentContainerId);*/
                await _jsRuntime.InvokeVoidAsync("ThreeJSInterop.resetCamera", _currentContainerId);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ThreeJS camera reset failed: {ex.Message}");
            }
        }

        public async Task Cleanup()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("ThreeJSInterop.cleanup", _currentContainerId);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ThreeJS camera reset failed: {ex.Message}");
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_currentContainerId != null)
                {
                    /*await _threeJSModule.InvokeVoidAsync(
                        "cleanup",
                        _currentContainerId);*/
                    await _jsRuntime.InvokeVoidAsync("ThreeJSInterop.cleanup", _currentContainerId);
                }
                //await _threeJSModule.DisposeAsync();

                

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ThreeJS cleanup failed: {ex.Message}");
            }


            _isInitialized = false;
            _currentContainerId = null;
            _threeJSModule = null;
        }
    }
}