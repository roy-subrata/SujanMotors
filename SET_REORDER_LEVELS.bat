@echo off
echo Setting reorder levels for low stock alerts...
echo.

echo Setting PRESSURE PLATE reorder level to 5...
curl -X PUT http://localhost:5292/api/stock/levels/152c70bd-805f-4b00-b3db-54c6210358c5 -H "Content-Type: application/json" -d "{\"reorderLevel\": 5, \"reorderQuantity\": 10}"
echo.
echo.

echo Setting Looking Glass reorder level to 15...
curl -X PUT http://localhost:5292/api/stock/levels/934bc574-d1ff-44fa-9478-8888bf2bf68c -H "Content-Type: application/json" -d "{\"reorderLevel\": 15, \"reorderQuantity\": 20}"
echo.
echo.

echo Done! Now refresh the browser page.
echo.
pause
