using Cutter.Data;
using System.Numerics;

namespace Cutter.Services
{
    public class CuttingState
    {

        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public CuttingPlan? CurrentPlan { get; private set; }
        public List<Sheet>? CurrentSheet { get; private set; }
        public List<CuttingPlan> ListPlan { get; set; }
        public event Action? OnChange;
        public event Func<Task>? OnChangeAsync;

        public void SetPlan(CuttingPlan plan, List<Sheet> sheet)
        {
            CurrentPlan = plan;
            //CurrentSheet = sheet;
            CurrentSheet = new List<Sheet>(sheet);
            NotifyStateChangedAsync();
        }

        public void SetPlan(List<CuttingPlan> plan)
        {
            CurrentPlan = null;
            CurrentSheet = null;
            //ListPlan = plan;
            ListPlan = new List<CuttingPlan>(plan ?? new());
            NotifyStateChangedAsync();
        }

        public void ClearState()
        {
            CurrentPlan = null;
            CurrentSheet = null;
            ListPlan = null;
            NotifyStateChangedAsync();
        }

        private async Task NotifyStateChangedAsync()
        {
            OnChange?.Invoke();

            if (OnChangeAsync != null)
            {
                await OnChangeAsync.Invoke();
            }
        }

    }
}
