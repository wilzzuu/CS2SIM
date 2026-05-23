from PIL import Image
import os.path
import shutil

def add_prefix_to_images(source_folder, dest_folder, prefix="ST"):
    if not os.path.exists(source_folder):
        print(f"Source folder '{source_folder}' does not exist.")
        return
    
    # Check if the destination folder exists, if not, create it
    if not os.path.exists(dest_folder):
        os.makedirs(dest_folder)
        print(f"Destination folder '{dest_folder}' created.")

    # Iterate over files in the source folder
    for filename in os.listdir(source_folder):
        if filename.endswith('.png'):
            # Construct the new filename with prefix
            dest_path = os.path.join(dest_folder, filename)
            new_filename = os.path.splitext(filename)[0] + prefix + '.png'
            if os.path.exists(dest_path):
                print(f"Skipping {filename}. Already exists in {dest_folder}")
                continue
                
            # Open the image using Pillow
            image = Image.open(os.path.join(source_folder, filename))

            # Resize the image
            resized_image = image.resize((target_width, target_height))

            # Save the resized image to the destination folder
            resized_image.save(os.path.join(source_folder, filename))

            # Copy the file to the destination folder with prefix
            shutil.copy(os.path.join(source_folder, filename), os.path.join(dest_folder, new_filename))
            shutil.copy(os.path.join(source_folder, filename), os.path.join(dest_folder, filename))
            print(f"File '{filename}' copied and saved as '{new_filename}'")
            # Copy the file to the backup folder (original version)
            print(f"'{filename}' and '{new_filename}' copied to backup folder.")

            # Delete the original file
            os.remove(os.path.join(source_folder, filename))
            
            print(f"Original file '{filename}' deleted from processing folder.")
            meta_file_path = os.path.join(source_folder, filename + ".meta")
            if (os.path.exists(meta_file_path)):
                os.remove(meta_file_path)
                print(f"Metafile '{filename}.meta' deleted from processing folder.")
            
main_path = os.path.dirname(os.path.realpath(__file__))
root_path = os.path.abspath(os.path.join(main_path, os.pardir))
source_folder = os.path.join(root_path, f"Assets/StreamingAssets/pre_processed_item_images")
dest_folder = os.path.join(root_path, f"Assets/Resources/ItemImages/")
target_width = 300  # Set your desired width
target_height = 225  # Set your desired height

if __name__ == "__main__":
    add_prefix_to_images(source_folder, dest_folder)
