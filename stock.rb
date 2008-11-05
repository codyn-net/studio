require 'gtk2'

module Cpg
	class Stock
		STATE = :"cpg-state"
		LINK = :"cpg-link"
		CHAIN = :"cpg-chain"
		CHAIN_BROKEN = :"cpg-chain-broken"
	
		def self.factory
			@factory
		end
	
		def self.icon_path(s)
			File.join(File.dirname(__FILE__), 'icons', s)
		end

		Gtk::Stock.add(Stock::STATE, 'State')
		Gtk::Stock.add(Stock::LINK, 'Link')
		Gtk::Stock.add(Stock::CHAIN, 'Chain')
		Gtk::Stock.add(Stock::CHAIN_BROKEN, 'Chain Broken')
	
		@factory = Gtk::IconFactory.new
		@factory.add(Cpg::Stock::STATE.to_s, Gtk::IconSet.new(Gdk::Pixbuf.new(icon_path('state.png'))))
		@factory.add(Cpg::Stock::LINK.to_s, Gtk::IconSet.new(Gdk::Pixbuf.new(icon_path('link.png'))))
		@factory.add(Cpg::Stock::CHAIN.to_s, Gtk::IconSet.new(Gdk::Pixbuf.new(icon_path('chain.png'))))
		@factory.add(Cpg::Stock::CHAIN_BROKEN.to_s, Gtk::IconSet.new(Gdk::Pixbuf.new(icon_path('chain-broken.png'))))
		
		@factory.add_default
		
		def self.chain_button
			but = Gtk::ToggleButton.new
			but.relief = Gtk::RELIEF_NONE
			
			im1 = Gtk::Image.new(icon_path('chain-broken.png'))
			im2 = Gtk::Image.new(icon_path('chain.png'))
			
			but.active = false
			but.image = im1
			
			but.signal_connect('toggled') do |x| 
				but.image = x.active? ? im2 : im1
				
				yield but if block_given?
			end
			
			but
		end
		
		def self.close_button
			but = Gtk::Button.new
			
			rc = Gtk::RcStyle.new
			rc.ythickness = 0
			rc.xthickness = 0
			but.modify_style(rc)
			
			but.image = Gtk::Image.new(Gtk::Stock::CLOSE, Gtk::IconSize::MENU)
			but.relief = Gtk::ReliefStyle::NONE
			
			if block_given?
				but.signal_connect('clicked') { |b| yield b }
			end
			
			but
		end
	end
end
