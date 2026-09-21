import { Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '../../i18n/translate.pipe';
import { Wish } from '../../interfaces/wish';
import { skyDust, skyPoints } from './wish-sky-layout';

@Component({
    imports: [TranslatePipe],
    selector: 'app-wish-sky',
    styleUrl: './wish-sky.css',
    templateUrl: './wish-sky.html',
})
export class WishSky {
    readonly wishes = input.required<Wish[]>();
    readonly selectedId = input<number | null>(null);
    readonly flashId = input<number | null>(null);
    readonly selectWish = output<number>();

    readonly dust = skyDust(70);

    readonly stars = computed(() => {
        const points = skyPoints(this.wishes().map(w => w.id));

        return this.wishes().flatMap(wish => {
            const point = points.get(wish.id);
            return point ? [{ id: wish.id, title: wish.title, fulfilled: wish.isFulfilled, ...point }] : [];
        });
    });
}
