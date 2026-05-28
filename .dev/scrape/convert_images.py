from pathlib import Path
from PIL import Image


SCRIPT_DIR = Path(__file__).resolve().parent
IMAGE_DIR = SCRIPT_DIR / "data" / "IMAGE" / "item"

for image_file in IMAGE_DIR.glob("*.webp"):
    image_file.unlink(missing_ok=True)

