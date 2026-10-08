namespace _Project.Core.Building
{
    public interface IBuildingProgressStorage
    {
        BuildingAreaProgress Load(int areaIndex);

        void Save(int areaIndex, BuildingAreaProgress progress);
    }
}
