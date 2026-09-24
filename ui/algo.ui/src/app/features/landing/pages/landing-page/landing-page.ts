import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { catchError, finalize, of } from 'rxjs';

import { CategoriesApiService } from '../../../categories/api/categories-api.service';
import { CategoryDto } from '../../../categories/models/categories.models';

interface IconItem {
  readonly icon: string;
  readonly title: string;
  readonly text: string;
}

interface StatItem {
  readonly icon: string;
  readonly value: string;
  readonly label: string;
}

interface GameCard {
  readonly id: number | string;
  readonly image: string;
  readonly title: string;
  readonly label: string;
}

interface PromoCard {
  readonly icon: string;
  readonly title: string;
  readonly text: string;
  readonly image?: string;
}

@Component({
  selector: 'app-landing-page',
  imports: [CommonModule, RouterLink],
  template: `
    <main class="landing-page" dir="rtl">
      <section id="home" class="top-stage">
        <header class="site-header">
          <a class="brand" href="#" aria-label="المهندس">
         <img style="width: 102px;"src="https://fileserver.aljawharaplus.com/images/uploads%2Ftest%2FChatGPT%20Image%20Jul%203%2C%202026%2C%2006_52_51%20AM.png" alt="المهندس" />  
          </a>

          <nav class="main-nav" aria-label="روابط الصفحة">
            <a href="#">الرئيسية</a>
            <a href="#games">الألعاب</a>
            <a href="#cards">بطاقات وشركات</a>
            <a href="#offers">العروض</a>
            <a href="#support">تواصل معنا</a>
          </nav>

          <a routerLink="/auth/login" class="login-button">
            <i class="pi pi-user"></i>
            تسجيل دخول
          </a>
        </header>

        <div class="hero">
      
        <div class="hero-copy">
            <h1>
              اشحن ألعابك المفضلة
              <span>بسرعة وأمان</span>
            </h1>
            <p>أفضل تجربة شحن للاعبين مع دعم فوري وأسعار تنافسية</p>

            <div class="feature-row" aria-label="مميزات الخدمة">
              @for (feature of heroFeatures; track feature.title) {
                <div class="mini-feature">
                  <i [class]="feature.icon"></i>
                  <strong>{{ feature.title }}</strong>
                  <small>{{ feature.text }}</small>
                </div>
              }
            </div>

            <div class="hero-actions">
              <a class="primary-cta" href="#games">
                <i class="pi pi-bolt"></i>
                ابدأ الشحن الآن
              </a>
              <a class="secondary-cta" href="#games">
                <i class="pi pi-gamepad"></i>
                استكشف الألعاب
              </a>
            </div>
          </div>
          
        </div>

        <section class="stats-panel" aria-label="إحصائيات الخدمة">
          @for (stat of stats; track stat.label) {
            <article class="stat-item">
              <span><i [class]="stat.icon"></i></span>
              <div>
                <strong>{{ stat.value }}</strong>
                <small>{{ stat.label }}</small>
              </div>
            </article>
          }
        </section>
      </section>

      <section class="market-shell">
        <section id="games" class="games-section">
          <div class="games-toolbar">
            <div class="search-box">
              <i class="pi pi-search"></i>
              <input
                type="search"
                placeholder="ابحث عن لعبة أو بطاقة"
                aria-label="ابحث عن لعبة أو بطاقة"
                (input)="categorySearch.set($any($event.target).value)"
              />
            </div>

            <div class="section-title">
              <button type="button">عرض الكل</button>
              <h2>شحن الألعاب</h2>
            </div>
          </div>

          <div class="games-track" id="cards">
            <button class="slider-button" type="button" aria-label="السابق">
              <i class="pi pi-angle-right"></i>
            </button>
            @for (game of filteredGames(); track game.id) {
              <article class="game-card">
                <div class="game-image" [style.background-image]="'linear-gradient(180deg, rgba(2,8,8,.05), rgba(2,8,8,.78)), url(' + game.image + ')'">
                  <strong>{{ game.label }}</strong>
                </div>
                <h3>{{ game.title }}</h3>
                <a href="#games">
                  <i class="pi pi-bolt"></i>
                  اشحن الآن
                </a>
              </article>
            }
            @if (!loadingCategories() && filteredGames().length === 0) {
              <p class="games-empty">لا توجد تصنيفات مطابقة للبحث</p>
            }
            <button class="slider-button" type="button" aria-label="التالي">
              <i class="pi pi-angle-left"></i>
            </button>
          </div>
        </section>

        <section id="offers" class="promo-grid">
          @for (promo of promos; track promo.title) {
            <article class="promo-card" [class.has-image]="promo.image">
              @if (promo.image) {
                <img [src]="promo.image" [alt]="promo.title" />
              } @else {
                <span class="promo-icon"><i [class]="promo.icon"></i></span>
              }
              <div>
                <h3>{{ promo.title }}</h3>
                <p>{{ promo.text }}</p>
                <a href="#games">اعرف المزيد <i class="pi pi-angle-left"></i></a>
              </div>
            </article>
          }
        </section>
      </section>

      <footer class="landing-footer">
        <p>المهندس | جميع الحقوق محفوظة 2024</p>
        <div class="footer-highlights">
          <span><i class="pi pi-shield"></i> تشفير وحماية عالية</span>
          <span><i class="pi pi-check-circle"></i> شركاء رسميون ومعتمدون</span>
          <span><i class="pi pi-sparkles"></i> أسعار تنافسية</span>
        </div>
        <div class="socials" id="support">
          <a href="#" aria-label="تيليجرام"><i class="pi pi-telegram"></i></a>
          <a href="#" aria-label="ديسكورد"><i class="pi pi-discord"></i></a>
          <a href="#" aria-label="إنستغرام"><i class="pi pi-instagram"></i></a>
          <a href="#" aria-label="واتساب"><i class="pi pi-whatsapp"></i></a>
        </div>
      </footer>

      <nav class="mobile-bottom-nav" aria-label="التنقل السفلي">
        <a class="is-active" href="#home">
          <i class="pi pi-home"></i>
          <span>الرئيسية</span>
        </a>
        <a href="#games">
          <i class="pi pi-th-large"></i>
          <span>الألعاب</span>
        </a>
        <a href="#offers">
          <i class="pi pi-gift"></i>
          <span>العروض</span>
        </a>
        <a routerLink="/auth/login">
          <i class="pi pi-user"></i>
          <span>حسابي</span>
        </a>
      </nav>
    </main>
  `,
  styles: [],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LandingPage {
  private readonly categoriesApi = inject(CategoriesApiService);

  protected readonly categories = signal<CategoryDto[]>([]);
  protected readonly categorySearch = signal('');
  protected readonly loadingCategories = signal(false);
  protected readonly filteredGames = computed<readonly GameCard[]>(() => {
    const search = this.categorySearch().trim().toLocaleLowerCase();
    const source = this.categories().length > 0 ? this.categories().map((category) => this.toGameCard(category)) : this.fallbackGames;

    if (!search) {
      return source;
    }

    return source.filter((game) =>
      `${game.title} ${game.label}`.toLocaleLowerCase().includes(search)
    );
  });

  constructor(title: Title) {
    title.setTitle('المهندس | اشحن ألعابك المفضلة');
    this.loadCategories();
  }

  protected readonly heroFeatures: readonly IconItem[] = [
    { icon: 'pi pi-shield', title: 'أمان عالي', text: 'حماية بياناتك 100%' },
    { icon: 'pi pi-headphones', title: 'دعم فوري', text: '24/7 على مدار الساعة' },
    { icon: 'pi pi-user', title: 'أسعار تنافسية', text: 'عروض وخصومات حصرية' },
    { icon: 'pi pi-bolt', title: 'تسليم فوري', text: 'شحن رصيدك مباشرة' }
  ];

  protected readonly stats: readonly StatItem[] = [
    { icon: 'pi pi-headphones', value: '24/7', label: 'دعم فني متواصل' },
    { icon: 'pi pi-shield', value: '100%', label: 'آمن وموثوق' },
    { icon: 'pi pi-shopping-cart', value: '+1M', label: 'عملية شحن ناجحة' },
    { icon: 'pi pi-user', value: '+250K', label: 'عميل سعيد' }
  ];

  private readonly fallbackGames: readonly GameCard[] = [
    {
      id: 'fallback-pubg',
      title: 'PUBG Mobile',
      label: 'PUBG MOBILE',
      image: 'https://images.unsplash.com/photo-1542751371-adc38448a05e?auto=format&fit=crop&w=420&q=80'
    },
    {
      id: 'fallback-free-fire',
      title: 'Free Fire',
      label: 'FREE FIRE',
      image: 'https://images.unsplash.com/photo-1511512578047-dfb367046420?auto=format&fit=crop&w=420&q=80'
    },
    {
      id: 'fallback-mobile-legends',
      title: 'Mobile Legends',
      label: 'MOBILE LEGENDS',
      image: 'https://images.unsplash.com/photo-1560253023-3ec5d502959f?auto=format&fit=crop&w=420&q=80'
    },
    {
      id: 'fallback-playstation',
      title: 'PlayStation Store',
      label: 'PLAYSTATION',
      image: 'https://images.unsplash.com/photo-1606144042614-b2417e99c4e3?auto=format&fit=crop&w=420&q=80'
    },
    {
      id: 'fallback-xbox',
      title: 'Xbox Gift Card',
      label: 'XBOX',
      image: 'https://images.unsplash.com/photo-1621259182978-fbf93132d53d?auto=format&fit=crop&w=420&q=80'
    },
    {
      id: 'fallback-itunes',
      title: 'iTunes Gift Card',
      label: 'iTunes',
      image: 'https://images.unsplash.com/photo-1516321318423-f06f85e504b3?auto=format&fit=crop&w=420&q=80'
    },
    {
      id: 'fallback-google-play',
      title: 'Google Play',
      label: 'Google Play',
      image: 'https://images.unsplash.com/photo-1611162617474-5b21e879e113?auto=format&fit=crop&w=420&q=80'
    }
  ];

  private loadCategories(): void {
    this.loadingCategories.set(true);
    this.categoriesApi
      .getCategories()
      .pipe(
        catchError(() => of([] as CategoryDto[])),
        finalize(() => this.loadingCategories.set(false))
      )
      .subscribe((categories) => {
        this.categories.set(categories.filter((category) => !category.deletedAt && !category.trashedAt));
      });
  }

  private toGameCard(category: CategoryDto): GameCard {
    return {
      id: category.id,
      title: category.name,
      label: category.name,
      image: category.imageUrl || this.categoryFallbackImage(category.id)
    };
  }

  private categoryFallbackImage(categoryId: number): string {
    return this.fallbackGames[categoryId % this.fallbackGames.length].image;
  }

  protected readonly promos: readonly PromoCard[] = [
    { icon: 'pi pi-percentage', title: 'عروض حصرية كل يوم!', text: 'خصومات وباقات خاصة للاعبين' },
    { icon: 'pi pi-gift', title: 'نقاط مكافآت', text: 'اجمع النقاط واستبدلها' },
    { icon: 'pi pi-wallet', title: 'طرق دفع آمنة', text: 'متعددة وبسيطة' },
    {
      icon: 'pi pi-headphones',
      title: 'دعم فني متواصل',
      text: 'نحن هنا لمساعدتك',
      image: 'https://fileserver.aljawharaplus.com/images/uploads%2Ftest%2FChatGPT%20Image%20Jul%203%2C%202026%2C%2004_49_45%20AM.png'
    }
  ];
}
