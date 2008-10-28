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
	end
end
