"""Extract reusable environment sprites from the supplied map illustrations."""

from pathlib import Path

from PIL import Image, ImageDraw


ART = Path(__file__).resolve().parents[1] / "Unity/Assets/Art"
OUTPUT = ART / "MapPieces"

# Source boxes are measured from the upper-left corner and exclude neighboring props.
SLICES = {
    1: {
        "security-tile": ((950, 632, 1134, 807), (128, 128)),
        "security-wall": ((461, 132, 525, 250), (48, 64)),
        "security-screens": ((537, 142, 943, 288), (216, 79)),
        "security-rug": ((499, 648, 946, 880), (192, 100)),
        "steel-rail": ((202, 87, 370, 119), (96, 18)),
    },
    2: {
        "lobby-wood": ((552, 450, 887, 785), (192, 192)),
        "lobby-wall": ((1075, 139, 1248, 250), (128, 64)),
        "lobby-window": ((152, 140, 487, 281), (164, 69)),
        "lobby-sign": ((816, 146, 1064, 240), (140, 53)),
        "entry-mat": ((1292, 604, 1370, 687), (49, 52)),
    },
    3: {
        "storage-wood": ((807, 681, 934, 808), (96, 96)),
        "storage-aisle": ((438, 519, 576, 654), (128, 128)),
        "storage-wall": ((136, 128, 304, 206), (128, 64)),
        "storage-boxes": ((187, 320, 297, 354), (80, 25)),
    },
    4: {
        "staff-tile": ((315, 307, 478, 470), (128, 128)),
        "staff-aisle": ((594, 743, 684, 872), (64, 96)),
        "staff-wall": ((747, 139, 784, 258), (32, 64)),
        "staff-window": ((246, 110, 522, 216), (144, 55)),
    },
    5: {
        "counter-tile": ((623, 312, 814, 365), (128, 40)),
        "counter-wood": ((600, 563, 947, 910), (192, 192)),
        "counter-wall": ((155, 90, 308, 156), (128, 64)),
        "counter-face": ((321, 397, 418, 513), (85, 64)),
        "back-bar-face": ((367, 198, 443, 296), (64, 64)),
        "pastry-case": ((1132, 368, 1340, 453), (144, 59)),
        "steel-sink": ((1322, 118, 1396, 292), (48, 113)),
        "espresso-machine": ((515, 114, 644, 205), (85, 60)),
    },
    6: {
        "hall-wood": ((187, 536, 363, 608), (128, 52)),
        "hall-carpet": ((425, 379, 523, 426), (96, 46)),
        "hall-wall": ((736, 119, 755, 190), (16, 64)),
        "hall-window": ((386, 71, 714, 195), (192, 73)),
    },
    7: {
        "parking-asphalt": ((438, 421, 997, 650), (256, 112)),
        "parking-pavers": ((350, 291, 405, 446), (48, 128)),
        "parking-bay": ((911, 178, 1043, 402), (80, 136)),
        "parking-edge": ((797, 101, 1009, 166), (128, 64)),
        "parking-window": ((199, 285, 329, 404), (80, 73)),
        "parking-vents": ((184, 107, 324, 239), (80, 75)),
        "parking-flowers": ((524, 752, 783, 834), (144, 46)),
    },
    8: {
        "service-tile": ((645, 314, 970, 523), (192, 128)),
        "service-wall": ((646, 198, 950, 312), (192, 64)),
        "equipment-panel": ((360, 242, 418, 306), (58, 64)),
    },
    9: {
        # Only clean stone and pipework are sampled. The moving hazards live in the scene.
        "utility-tile": ((823, 315, 992, 513), (128, 144)),
        "utility-wall": ((1712, 100, 1953, 247), (192, 64)),
    },
}


def write(source, name, box, size, *, outline=None, rotate=False):
    piece = source.crop(box).convert("RGBA")
    if outline:
        mask = Image.new("L", piece.size)
        ImageDraw.Draw(mask).polygon(outline, fill=255)
        piece.putalpha(mask)
    if rotate:
        piece = piece.transpose(Image.Transpose.ROTATE_90)
    piece.resize(size, Image.Resampling.NEAREST).save(OUTPUT / f"{name}.png")


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    sources = {int(path.name[:2]): Image.open(path) for path in (ART / "maps").glob("*.png")}
    for number, slices in SLICES.items():
        for name, (box, size) in slices.items():
            write(sources[number], name, box, size)

    rail_box, _ = SLICES[1]["steel-rail"]
    write(sources[1], "steel-rail-side", rail_box, (18, 96), rotate=True)
    write(sources[6], "hall-table", (511, 234, 609, 346), (72, 82),
          outline=[(5, 2), (91, 2), (97, 8), (97, 85), (92, 91), (91, 107),
                   (83, 108), (82, 93), (16, 93), (15, 109), (8, 109),
                   (7, 91), (0, 85), (0, 9)])
    write(sources[6], "hall-chair", (472, 249, 511, 330), (33, 66),
          outline=[(5, 4), (11, 3), (14, 10), (37, 12), (38, 64), (33, 71),
                   (9, 69), (8, 79), (2, 78), (1, 62), (0, 18)])

    write(sources[7], "car-silver", (546, 225, 650, 385), (114, 74), rotate=True,
          outline=[(24, 3), (80, 3), (94, 16), (96, 44), (103, 45), (103, 53),
                   (96, 54), (98, 140), (87, 155), (16, 156), (8, 146), (6, 54),
                   (0, 53), (0, 45), (8, 43), (10, 16)])
    write(sources[7], "car-blue", (681, 225, 793, 385), (114, 80), rotate=True,
          outline=[(26, 3), (81, 3), (96, 15), (101, 41), (110, 43), (110, 51),
                   (103, 53), (104, 140), (94, 153), (18, 155), (10, 145),
                   (8, 53), (1, 52), (1, 44), (9, 41), (13, 17)])


if __name__ == "__main__":
    main()
