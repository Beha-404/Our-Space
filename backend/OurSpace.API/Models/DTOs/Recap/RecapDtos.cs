using OurSpace.API.Models.DTOs.Memory;

namespace OurSpace.API.Models.DTOs.Recap;

public record RecapDto(
    int Year,
    int Photos,
    int AudioMessages,
    int Events,
    int WishesFulfilled,
    int CapsulesSealed,
    List<int> MemoriesPerMonth,
    List<MemoryRow> Highlights,
    List<int> AvailableYears
);
