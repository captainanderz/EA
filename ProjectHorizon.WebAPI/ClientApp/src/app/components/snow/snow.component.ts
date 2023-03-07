import { Component, Input, ElementRef, HostListener } from '@angular/core';

@Component({
  selector: 'Snow',
  template: '<canvas class="snow-canvas"></canvas>',
  styles: [
    `
      .snow-canvas {
        position: absolute;
        left: 0;
        top: 0;
        width: 100%;
        height: 100%;
        pointer-events: none;
      }
    `,
  ],
})
export class SnowComponent {
  @Input('amount') amount: number = 50;
  @Input('size') size: number = 5;
  @Input('speed') speed: number = 1.5;
  @Input('wind') wind: number = 0;
  @Input('color') color: string = '#fff';
  @Input('opacity') opacity: number = 0.8;
  @Input('swing') swing: number = 1;
  @Input('image') image?: string = undefined;
  @Input('zIndex') zIndex?: number = undefined;

  private inited: boolean = false;
  private canvas: any = null;
  private context: any = null;
  private flakes: Array<any> = [];
  private canvasWidth: number = 0;
  private canvasHeight: number = 0;
  private img: any = null;

  constructor(private elementRef: ElementRef) {}

  @HostListener('window:resize', ['$event'])
  onResize() {
    this.handleResize();
  }

  ngOnInit() {
    this.canvas = this.elementRef.nativeElement.querySelector('.snow-canvas');
    this.context = this.canvas.getContext('2d');
    this.canvasHeight = this.canvas.offsetHeight;
    this.canvasWidth = this.canvas.offsetWidth;

    this.canvas.height = this.canvasHeight;
    this.canvas.width = this.canvasWidth;
    if (this.zIndex != null) this.canvas.style.zIndex = this.zIndex;

    if (this.image) {
      this.img = new Image();
      this.img.onload = () => this.init();
      this.img.src = this.image;
    } else {
      this.init();
    }
  }

  ngOnChanges(changes: {
    amount: { firstChange: any };
    zIndex: any;
    image: any;
  }) {
    if (changes.amount && changes.amount.firstChange) return;
    if (changes.zIndex) this.canvas.style.zIndex = this.zIndex;
    if (changes.amount) this.update();
    if (changes.image) {
      this.img = new Image();
      this.img.src = this.image;
    }
  }

  /**
   * update props
   */
  update() {
    if (this.image) {
      this.img = new Image();
      this.img.onload = () => this.init();
      this.img.src = this.image;
    } else {
      this.init();
    }
  }

  /**
   * Initialize flakes
   */
  init() {
    this.flakes = [];
    for (var i = 0; i < this.amount; i++) {
      this.flakes.push({
        x: this.random(0, this.canvasWidth),
        y: this.random(0, this.canvasHeight),
        r: this.random(this.size, this.size * 2) / 2,
        velX: 0,
        velY: this.random(this.speed, this.speed * 2),
        swing: this.random(0, 2 * Math.PI),
        opacity: this.random(0, this.opacity),
      });
    }
    if (!this.inited) {
      this.inited = true;
      this.snow();
    }
  }

  /**
   * draw snowlakes on canvas
   */
  snow() {
    this.context.clearRect(0, 0, this.canvasWidth, this.canvasHeight);
    for (var i = 0; i < this.flakes.length; i++) {
      var flake = this.flakes[i];

      if (!this.image) {
        this.context.beginPath();
        this.context.fillStyle =
          'rgba(' + this.getRgb(this.color) + ',' + flake.opacity + ')';
        this.context.arc(flake.x, flake.y, flake.r, 2 * Math.PI, false);
        this.context.fill();
        this.context.closePath();
      } else {
        this.context.drawImage(
          this.img,
          flake.x - flake.r,
          flake.y - flake.r,
          2 * flake.r,
          2 * flake.r
        );
      }

      flake.velX =
        Math.abs(flake.velX) < Math.abs(this.wind)
          ? flake.velX + this.wind / 20
          : this.wind;
      flake.y = flake.y + flake.velY * 0.5;
      flake.x =
        flake.x +
        (this.swing
          ? 0.4 * Math.cos((flake.swing += 0.03)) * flake.opacity * this.swing
          : 0) +
        flake.velX * 0.5;
      if (
        flake.x > this.canvasWidth + flake.r ||
        flake.x < -flake.r ||
        flake.y > this.canvasHeight + flake.r ||
        flake.y < -flake.r
      ) {
        this.reset(flake);
      }
    }
    requestAnimationFrame(this.snow.bind(this));
  }

  /**
   * reset flake property
   * @param flake
   */
  reset(flake: {
    r: number;
    x: number;
    y: number;
    velX: number;
    velY: any;
    swing: any;
    opacity: any;
  }) {
    var prevR = flake.r;
    flake.r = this.random(this.size, this.size * 2) / 2;
    if (flake.x > this.canvasWidth + prevR) {
      flake.x = -flake.r;
      flake.y = this.random(0, this.canvasHeight);
    } else if (flake.x < -prevR) {
      flake.x = this.canvasWidth + flake.r;
      flake.y = this.random(0, this.canvasHeight);
    } else {
      flake.x = this.random(0, this.canvasWidth);
      flake.y = -flake.r;
    }
    flake.velX = 0;
    flake.velY = this.random(this.speed, this.speed * 2);
    flake.swing = this.random(0, 2 * Math.PI);
    flake.opacity = this.random(0, this.opacity);
  }

  /**
   * handle window resize
   */
  handleResize() {
    if (!this.inited) return;
    var H0 = this.canvas.height,
      W0 = this.canvas.width,
      H1 = this.canvas.offsetHeight,
      W1 = this.canvas.offsetWidth;

    this.canvas.height = this.canvasHeight = H1;
    this.canvas.width = this.canvasWidth = W1;
    for (var i = 0; i < this.flakes.length; i++) {
      var flake = this.flakes[i];
      flake.x = (flake.x / W0) * W1;
      flake.y = (flake.y / H0) * H1;
    }
  }

  /**
   * get random number
   * @param min
   * @param max
   */
  random(min: number, max: number) {
    var delta = max - min;
    return max === min ? min : Math.random() * delta + min;
  }

  /**
   * get rgb color
   * @param str
   */
  getRgb(str: string) {
    var rgb = '';
    if (str.indexOf('#') === 0) {
      rgb =
        str.length === 4
          ? str
              .substr(1)
              .split('')
              .map(function (n: string) {
                return parseInt(n.concat(n), 16);
              })
              .join(',')
          : str.length === 7
          ? [str.substr(1, 2), str.substr(3, 2), str.substr(5, 2)]
              .map(function (n) {
                return parseInt(n, 16);
              })
              .join(',')
          : '255,255,255';
    } else if (str.indexOf('rgb(') === 0) {
      rgb = str.substring(4, str.length - 1);
    } else {
      rgb = '255,255,255';
    }
    return rgb;
  }
}
