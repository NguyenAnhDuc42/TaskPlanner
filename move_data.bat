@echo off
echo Creating Application\Data\Configurations directory...
mkdir "C:\Users\Duc\Desktop\TaskPlanner\server\Application\Data\Configurations" 2>nul

echo Moving TaskPlanDbContext.cs...
move /Y "C:\Users\Duc\Desktop\TaskPlanner\server\Infrastructure\Data\TaskPlanDbContext.cs" "C:\Users\Duc\Desktop\TaskPlanner\server\Application\Data\" >nul

echo Flattening Configurations... (Removing category folders)
for /r "C:\Users\Duc\Desktop\TaskPlanner\server\Infrastructure\Data\Configurations" %%f in (*.*) do (
    move /Y "%%f" "C:\Users\Duc\Desktop\TaskPlanner\server\Application\Data\Configurations\" >nul
)

echo Moving Migrations folder...
move /Y "C:\Users\Duc\Desktop\TaskPlanner\server\Infrastructure\Migrations" "C:\Users\Duc\Desktop\TaskPlanner\server\Application\" >nul

echo.
echo Cleaning up old empty folders in Infrastructure...
rmdir /S /Q "C:\Users\Duc\Desktop\TaskPlanner\server\Infrastructure\Data\Configurations" 2>nul

echo.
echo SUCCESS! Everything is moved and flattened. 
echo Please close this window and tell me "Done" in the chat!
pause
