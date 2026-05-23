import shutil
import os.path
import re

def rename_image(source_folder, dest_folder):
    if not os.path.exists(source_folder):
        print(f"Source folder '{source_folder}' does not exist.")
        return
    
    if not os.path.exists(dest_folder):
        os.makedirs(dest_folder)
        print(f"Destination folder '{dest_folder}' created")

    for filename in os.listdir(source_folder):
        if filename.endswith('.png'):
            dest_path = os.path.join(source_folder, filename)
            filename_snd = filename.split('_')[1]
            re_split = re.split(r'(?<=\d)\D', filename_snd, maxsplit=1)
            item_nr = re_split[0]
            item_cond = filename_snd.split(item_nr)[1]
            item_nr = int(item_nr) + 1
            
            new_filename = "2_" + os.path.splitext(str(item_nr))[0] + item_cond
            shutil.copy(os.path.join(source_folder, filename), os.path.join(dest_folder, new_filename))
            print(f"File '{filename}' copied and saved as '{new_filename}'")



main_path = os.path.dirname(os.path.realpath(__file__))
root_path = os.path.abspath(os.path.join(main_path, os.pardir))
source_folder = os.path.join(root_path, f"Assets/StreamingAssets/pre_processed_item_images")
dest_folder = os.path.join(root_path, f"Assets/StreamingAssets/pre_processed_item_images/moved")

if __name__ == "__main__":
    rename_image(source_folder, dest_folder)